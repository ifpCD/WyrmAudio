using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

[BurstCompile(CompileSynchronously = true)]
internal unsafe struct EncodeSimpleAmbisonicsJob : IJobParallelForTransform
{
    public float3 ListenerPosition;

    [ReadOnly]
    public NativeArray<byte> Types;

    [ReadOnly]
    public NativeArray<AmbiHandle> GeneratorHandles;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandGains;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandSpreads;

    [ReadOnly]
    public NativeArray<float> GeneratorHorizontalSpreads;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> GeneratorOutputs;

    public void Execute(int index, TransformAccess transform)
    {
        float3 arrival =
            Types[index] == (byte)SimpleAmbisonicType.Positional
                ? (float3)transform.position - ListenerPosition
                : math.rotate((quaternion)transform.rotation, new float3(0f, 0f, 1f));

        float* basis = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];
        SphericalHarmonics.EvaluateArrival(arrival, basis);

        int generator = GeneratorHandleToSoA[GeneratorHandles[index].Index];
        float* target = (float*)GeneratorOutputs.GetUnsafePtr() + generator * HC.AMBISONIC_BUFFER_LENGTH;

        float3 gains = GeneratorBandGains[generator];
        float3 spreads = GeneratorBandSpreads[generator];
        float horizontalSpread = GeneratorHorizontalSpreads[generator];

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
            SphericalHarmonics.Shape(basis, gains[band], spreads[band], horizontalSpread, target + band * HC.MAX_AMBISONIC_CHANNELS);
    }
}

[BurstCompile(CompileSynchronously = true)]
internal struct ReadMeshTargetsJob : IJobParallelForTransform
{
    public float3 ListenerPosition;

    [WriteOnly]
    public NativeArray<float4x4> LocalToAmbisonic;

    public void Execute(int index, TransformAccess transform)
    {
        float4x4 localToListener = math.mul(float4x4.Translate(-ListenerPosition), (float4x4)transform.localToWorldMatrix);
        LocalToAmbisonic[index] = math.mul(SphericalHarmonics.AmbisonicFromUnity, localToListener);
    }
}

[BurstCompile(CompileSynchronously = true)]
internal unsafe struct ProjectMeshChunksJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int3> Chunks;

    [ReadOnly]
    public NativeArray<float4x4> LocalToAmbisonic;

    [ReadOnly]
    public NativeArray<float3x3> Triangles;

    [ReadOnly]
    public NativeArray<float3> TriangleBands;

    [ReadOnly]
    public NativeArray<double4> DirectionsX;

    [ReadOnly]
    public NativeArray<double4> DirectionsY;

    [ReadOnly]
    public NativeArray<double4> DirectionsZ;

    [NativeDisableParallelForRestriction]
    public NativeArray<double4> ChunkMoments;

    public void Execute(int index)
    {
        int3 chunk = Chunks[index];
        float4x4 localToAmbisonic = LocalToAmbisonic[chunk.x];

        double4* moments = (double4*)ChunkMoments.GetUnsafePtr() + index * PolygonProjection.BANDED_MOMENTS;
        UnsafeUtility.MemClear(moments, PolygonProjection.BANDED_MOMENTS * sizeof(double4));

        double4* scratch = stackalloc double4[PolygonProjection.SCRATCH];
        double4* directionsX = (double4*)DirectionsX.GetUnsafeReadOnlyPtr();
        double4* directionsY = (double4*)DirectionsY.GetUnsafeReadOnlyPtr();
        double4* directionsZ = (double4*)DirectionsZ.GetUnsafeReadOnlyPtr();

        for (int triangle = chunk.y; triangle < chunk.y + chunk.z; triangle++)
        {
            float3x3 vertices = Triangles[triangle];

            PolygonProjection.AccumulateTriangle(
                math.transform(localToAmbisonic, vertices.c0),
                math.transform(localToAmbisonic, vertices.c1),
                math.transform(localToAmbisonic, vertices.c2),
                TriangleBands[triangle],
                directionsX,
                directionsY,
                directionsZ,
                scratch,
                moments
            );
        }
    }
}

[BurstCompile(CompileSynchronously = true)]
internal unsafe struct ReduceMeshMomentsJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> ChunkOffsets;

    [ReadOnly]
    public NativeArray<int> ChunkCounts;

    [ReadOnly]
    public NativeArray<double4> ChunkMoments;

    [ReadOnly]
    public NativeArray<double4> MomentToHarmonic;

    [ReadOnly]
    public NativeArray<AmbiHandle> GeneratorHandles;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandGains;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandSpreads;

    [ReadOnly]
    public NativeArray<float> GeneratorHorizontalSpreads;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> GeneratorOutputs;

    public void Execute(int row)
    {
        double4* total = stackalloc double4[PolygonProjection.BANDED_MOMENTS];
        UnsafeUtility.MemClear(total, PolygonProjection.BANDED_MOMENTS * sizeof(double4));

        double4* chunkMoments = (double4*)ChunkMoments.GetUnsafeReadOnlyPtr() + ChunkOffsets[row] * PolygonProjection.BANDED_MOMENTS;

        for (int chunk = 0; chunk < ChunkCounts[row]; chunk++, chunkMoments += PolygonProjection.BANDED_MOMENTS)
        {
            for (int index = 0; index < PolygonProjection.BANDED_MOMENTS; index++)
                total[index] += chunkMoments[index];
        }

        int generator = GeneratorHandleToSoA[GeneratorHandles[row].Index];
        float* target = (float*)GeneratorOutputs.GetUnsafePtr() + generator * HC.AMBISONIC_BUFFER_LENGTH;

        double4* momentToHarmonic = (double4*)MomentToHarmonic.GetUnsafeReadOnlyPtr();
        double* harmonics = stackalloc double[HC.MAX_AMBISONIC_CHANNELS];
        float* distribution = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];

        float3 gains = GeneratorBandGains[generator];
        float3 spreads = GeneratorBandSpreads[generator];
        float horizontalSpread = GeneratorHorizontalSpreads[generator];

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            float* bandTarget = target + band * HC.MAX_AMBISONIC_CHANNELS;
            PolygonProjection.Resolve(total + band * PolygonProjection.MOMENTS, momentToHarmonic, harmonics);

            if (harmonics[0] <= 0.0)
            {
                UnsafeUtility.MemClear(bandTarget, HC.MAX_AMBISONIC_CHANNELS * sizeof(float));
                continue;
            }

            // unit-mass direction distribution: c00 == Y00, identical energy scale to a point source
            double normalization = SphericalHarmonics.Y00 / harmonics[0];

            for (int channel = 0; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
                distribution[channel] = (float)(harmonics[channel] * normalization);

            SphericalHarmonics.Shape(distribution, gains[band], spreads[band], horizontalSpread, bandTarget);
        }
    }
}

// Runs synchronously at registration. Every surface layer emits (no self-occlusion); vertex rgb optionally weights the bands.
[BurstCompile(CompileSynchronously = true)]
internal struct CacheMeshTrianglesJob : IJob
{
    [ReadOnly]
    public Mesh.MeshData Source;

    public bool VertexColorBands;

    public NativeList<float3x3> Triangles;
    public NativeList<float3> TriangleBands;

    public void Execute()
    {
        var vertices = new NativeArray<Vector3>(Source.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var colors = new NativeArray<Color>(VertexColorBands ? Source.vertexCount : 0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        Source.GetVertices(vertices);

        if (VertexColorBands)
            Source.GetColors(colors);

        for (int submesh = 0; submesh < Source.subMeshCount; submesh++)
        {
            SubMeshDescriptor descriptor = Source.GetSubMesh(submesh);

            if (descriptor.topology != MeshTopology.Triangles)
                continue;

            var indices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            Source.GetIndices(indices, submesh);

            for (int corner = 0; corner + 2 < indices.Length; corner += 3)
            {
                int a = indices[corner];
                int b = indices[corner + 1];
                int c = indices[corner + 2];

                Triangles.Add(new float3x3((float3)vertices[a], (float3)vertices[b], (float3)vertices[c]));
                TriangleBands.Add(VertexColorBands ? (Bands(colors[a]) + Bands(colors[b]) + Bands(colors[c])) / 3f : new float3(1f));
            }
        }
    }

    static float3 Bands(Color color) => new(color.r, color.g, color.b);
}

[BurstCompile]
public struct LoadAmbisonicOutputsToSources : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<AmbiHandle> SourceToAmbisonicGeneratorHandles;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoAIndex;

    [ReadOnly]
    public NativeArray<int> GeneratorVersions;

    [ReadOnly]
    public NativeArray<float> AmbisonicGeneratorBuffers;

    [NativeDisableParallelForRestriction]
    [WriteOnly]
    public NativeArray<float> SourceAmbisonicBuffers;

    public void Execute(int sourceIndex)
    {
        int sourceBufferOffset = HC.AMBISONIC_BUFFER_LENGTH * sourceIndex;

        AmbiHandle handle = SourceToAmbisonicGeneratorHandles[sourceIndex];
        if (handle.IsNull || GeneratorVersions[handle.Index] != handle.Version)
        {
            for (int i = 0; i < HC.AMBISONIC_BUFFER_LENGTH; i++)
                SourceAmbisonicBuffers[sourceBufferOffset + i] = 0f;

            return;
        }

        int ambisonicGeneratorIndex = GeneratorHandleToSoAIndex[handle.Index];

        int ambisonicBufferOffset = HC.AMBISONIC_BUFFER_LENGTH * ambisonicGeneratorIndex;

        NativeArray<float>.Copy(
            AmbisonicGeneratorBuffers,
            ambisonicBufferOffset,
            SourceAmbisonicBuffers,
            sourceBufferOffset,
            HC.AMBISONIC_BUFFER_LENGTH
        );
    }
}

// No generator registry: every source is unbound, which the load job resolves to silence.
[BurstCompile]
public struct ClearSourceAmbisonicBuffers : IJob
{
    public int Length;

    [WriteOnly]
    public NativeArray<float> SourceAmbisonicBuffers;

    public unsafe void Execute() => UnsafeUtility.MemClear(SourceAmbisonicBuffers.GetUnsafePtr(), (long)Length * sizeof(float));
}
