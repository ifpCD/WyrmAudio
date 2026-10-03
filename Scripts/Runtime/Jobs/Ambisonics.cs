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
    public NativeArray<int3> GeneratorBandOrders;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandGains;

    [ReadOnly]
    public NativeArray<float3> GeneratorBandSpreads;

    [ReadOnly]
    public NativeArray<float> GeneratorHorizontalSpreads;

    [ReadOnly]
    public NativeArray<DirectPropagation> GeneratorPropagations;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> GeneratorOutputs;

    public void Execute(int index, TransformAccess transform)
    {
        bool positional = Types[index] == (byte)SimpleAmbisonicType.Positional;
        float3 arrival = positional
            ? (float3)transform.position - ListenerPosition
            : math.rotate((quaternion)transform.rotation, new float3(0f, 0f, 1f));

        int generator = GeneratorHandleToSoA[GeneratorHandles[index].Index];
        int3 orders = GeneratorBandOrders[generator];

        float* basis = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];
        SphericalHarmonics.EvaluateArrival(arrival, math.cmax(orders), basis);

        float* target = AmbisonicBuffer.Get(GeneratorOutputs, generator);

        float3 gains = GeneratorBandGains[generator];
        float3 spreads = GeneratorBandSpreads[generator];
        float horizontalSpread = GeneratorHorizontalSpreads[generator];

        if (positional)
            gains *= GeneratorPropagations[generator].Evaluate(math.length(arrival));

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
            SphericalHarmonics.Shape(basis, orders[band], gains[band], spreads[band], horizontalSpread, AmbisonicBuffer.GetBand(target, band));
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
    public NativeArray<AmbiHandle> GeneratorHandles;

    [ReadOnly]
    public NativeArray<double4> DirectionsX;

    [ReadOnly]
    public NativeArray<double4> DirectionsY;

    [ReadOnly]
    public NativeArray<double4> DirectionsZ;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<DirectPropagation> GeneratorPropagations;

    [NativeDisableParallelForRestriction]
    public NativeArray<double4> ChunkMoments;

    [WriteOnly]
    public NativeArray<double3> ChunkMasses;

    public void Execute(int index)
    {
        int3 chunk = Chunks[index];
        float4x4 localToAmbisonic = LocalToAmbisonic[chunk.x];
        DirectPropagation propagation = GeneratorPropagations[GeneratorHandleToSoA[GeneratorHandles[chunk.x].Index]];

        double4* moments = BandedMoments.Get(ChunkMoments, index);
        BandedMoments.Clear(moments);
        double3 mass = 0.0;

        double4* scratch = stackalloc double4[PolygonProjection.SCRATCH];
        double4* directionsX = (double4*)DirectionsX.GetUnsafeReadOnlyPtr();
        double4* directionsY = (double4*)DirectionsY.GetUnsafeReadOnlyPtr();
        double4* directionsZ = (double4*)DirectionsZ.GetUnsafeReadOnlyPtr();

        for (int triangle = chunk.y; triangle < chunk.y + chunk.z; triangle++)
        {
            float3x3 vertices = Triangles[triangle];

            PolygonProjection.ProjectTriangle(
                math.transform(localToAmbisonic, vertices.c0),
                math.transform(localToAmbisonic, vertices.c1),
                math.transform(localToAmbisonic, vertices.c2),
                directionsX,
                directionsY,
                directionsZ,
                scratch,
                out double solidAngle,
                out double distance
            );

            if (solidAngle == 0.0)
                continue;

            float3 bands = TriangleBands[triangle];
            mass += solidAngle * (double3)bands;

            PolygonProjection.Accumulate(scratch, bands * propagation.Evaluate((float)distance), moments);
        }

        ChunkMasses[index] = mass;
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
    public NativeArray<double3> ChunkMasses;

    [ReadOnly]
    public NativeArray<double4> MomentToHarmonic;

    [ReadOnly]
    public NativeArray<AmbiHandle> GeneratorHandles;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<int3> GeneratorBandOrders;

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
        BandedMoments.Clear(total);
        double3 mass = 0.0;

        int firstChunk = ChunkOffsets[row];

        for (int chunk = firstChunk; chunk < firstChunk + ChunkCounts[row]; chunk++)
        {
            double4* chunkMoments = BandedMoments.GetReadOnly(ChunkMoments, chunk);

            for (int index = 0; index < PolygonProjection.BANDED_MOMENTS; index++)
                total[index] += chunkMoments[index];

            mass += ChunkMasses[chunk];
        }

        int generator = GeneratorHandleToSoA[GeneratorHandles[row].Index];
        float* target = AmbisonicBuffer.Get(GeneratorOutputs, generator);

        double4* momentToHarmonic = (double4*)MomentToHarmonic.GetUnsafeReadOnlyPtr();
        double* harmonics = stackalloc double[HC.MAX_AMBISONIC_CHANNELS];
        float* distribution = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];

        int3 orders = GeneratorBandOrders[generator];
        float3 gains = GeneratorBandGains[generator];
        float3 spreads = GeneratorBandSpreads[generator];
        float horizontalSpread = GeneratorHorizontalSpreads[generator];

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            float* bandTarget = AmbisonicBuffer.GetBand(target, band);
            int channels = SphericalHarmonics.ChannelCount(orders[band]);

            if (mass[band] <= 0.0)
            {
                AmbisonicBuffer.ClearBand(bandTarget);
                continue;
            }

            PolygonProjection.Resolve(BandedMoments.GetBand(total, band), momentToHarmonic, channels, harmonics);

            // per unit solid angle: c00 == Y00 * mean propagation gain, identical energy scale to a point source
            double normalization = 1.0 / mass[band];

            for (int channel = 0; channel < channels; channel++)
                distribution[channel] = (float)(harmonics[channel] * normalization);

            SphericalHarmonics.Shape(distribution, orders[band], gains[band], spreads[band], horizontalSpread, bandTarget);
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
        NativeArray<Vector3> vertices = new(Source.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        int vertexCount = Source.vertexCount;
        NativeArray<Color> colors = new(VertexColorBands ? vertexCount : 0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

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
