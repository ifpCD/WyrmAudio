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

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> GeneratorOutputs;

    public void Execute(int index, TransformAccess transform)
    {
        float3 arrival =
            Types[index] == (byte)SimpleAmbisonicType.Positional
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

        double4* moments = BandedMoments.Get(ChunkMoments, index);
        BandedMoments.Clear(moments);

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

        int firstChunk = ChunkOffsets[row];

        for (int chunk = firstChunk; chunk < firstChunk + ChunkCounts[row]; chunk++)
        {
            double4* chunkMoments = BandedMoments.GetReadOnly(ChunkMoments, chunk);

            for (int index = 0; index < PolygonProjection.BANDED_MOMENTS; index++)
                total[index] += chunkMoments[index];
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

            PolygonProjection.Resolve(BandedMoments.GetBand(total, band), momentToHarmonic, channels, harmonics);

            if (harmonics[0] <= 0.0)
            {
                AmbisonicBuffer.ClearBand(bandTarget);
                continue;
            }

            // unit-mass direction distribution: c00 == Y00, identical energy scale to a point source
            double normalization = SphericalHarmonics.Y00 / harmonics[0];

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

// Stale or unbound generators resolve to silence: order 0 with a zero omnidirectional term.
[BurstCompile]
internal unsafe struct LoadAmbisonicOutputsToSourcesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<AmbiHandle> SourceGeneratorHandles;

    [ReadOnly]
    public NativeArray<float3> SourceBandGains;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<int> GeneratorVersions;

    [ReadOnly]
    public NativeArray<int3> GeneratorBandOrders;

    [ReadOnly]
    public NativeArray<float> GeneratorOutputs;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> SourceOutputs;

    [WriteOnly]
    public NativeArray<int3> SourceBandOrders;

    public void Execute(int source)
    {
        float* target = AmbisonicBuffer.Get(SourceOutputs, source);
        AmbiHandle handle = SourceGeneratorHandles[source];

        if (handle.IsNull || GeneratorVersions[handle.Index] != handle.Version)
        {
            AmbisonicBuffer.Clear(target);
            SourceBandOrders[source] = int3.zero;
            return;
        }

        int generator = GeneratorHandleToSoA[handle.Index];
        float* field = AmbisonicBuffer.GetReadOnly(GeneratorOutputs, generator);
        float3 gains = SourceBandGains[source];

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            float gain = gains[band];
            float* bandField = AmbisonicBuffer.GetBand(field, band);
            float* bandTarget = AmbisonicBuffer.GetBand(target, band);

            for (int channel = 0; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
                bandTarget[channel] = bandField[channel] * gain;
        }

        SourceBandOrders[source] = GeneratorBandOrders[generator];
    }
}

// No generator registry: every source is unbound.
[BurstCompile]
internal unsafe struct ClearSourceAmbisonicsJob : IJob
{
    public int SourceCount;

    [WriteOnly]
    public NativeArray<float> SourceOutputs;

    [WriteOnly]
    public NativeArray<int3> SourceBandOrders;

    public void Execute()
    {
        AmbisonicBuffer.Clear(SourceOutputs, 0, SourceCount);
        UnsafeUtility.MemClear(SourceBandOrders.GetUnsafePtr(), (long)SourceCount * sizeof(int3));
    }
}
