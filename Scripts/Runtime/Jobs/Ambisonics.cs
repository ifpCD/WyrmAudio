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
        AmbiHandle handle = GeneratorHandles[index];
        int generatorIdx = GeneratorHandleToSoA[handle.Index];

        float3 gains = GeneratorBandGains[generatorIdx];

        if (Types[index] == (byte)SimpleAmbisonicType.Directional)
        {
            float3 forward = math.rotate((quaternion)transform.rotation, new float3(0f, 0f, 1f));
            Encode(generatorIdx, forward, gains);
            return;
        }

        float3 arrival = (float3)transform.position - ListenerPosition;
        DirectPropagation propagation = GeneratorPropagations[generatorIdx];

        Encode(generatorIdx, arrival, gains * propagation.Evaluate(math.length(arrival)));
    }

    void Encode(int generatorIdx, float3 arrival, float3 gains)
    {
        int3 orders = GeneratorBandOrders[generatorIdx];
        float3 spreads = GeneratorBandSpreads[generatorIdx];
        float horizontalSpread = GeneratorHorizontalSpreads[generatorIdx];

        float* basis = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];
        SphericalHarmonics.EvaluateArrival(arrival, math.cmax(orders), basis);

        float* target = AmbisonicBuffer.Get(GeneratorOutputs, generatorIdx);

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

        AmbiHandle handle = GeneratorHandles[chunk.x];
        int generatorIdx = GeneratorHandleToSoA[handle.Index];

        DirectPropagation propagation = GeneratorPropagations[generatorIdx];

        double4* moments = BandedMoments.Get(ChunkMoments, index);
        BandedMoments.Clear(moments);

        ChunkMasses[index] = ProjectTriangles(chunk, propagation, moments);
    }

    // accumulates the propagated moments; returns the per band solid angle before propagation
    double3 ProjectTriangles(int3 chunk, DirectPropagation propagation, double4* moments)
    {
        float4x4 localToAmbisonic = LocalToAmbisonic[chunk.x];
        double4* scratch = stackalloc double4[PolygonProjection.SCRATCH];
        double3 mass = 0.0;

        for (int triangle = chunk.y; triangle < chunk.y + chunk.z; triangle++)
        {
            ProjectTriangle(triangle, localToAmbisonic, scratch, out double solidAngle, out double distance);

            if (solidAngle == 0.0)
                continue;

            float3 bands = TriangleBands[triangle];
            mass += solidAngle * (double3)bands;

            PolygonProjection.Accumulate(scratch, bands * propagation.Evaluate((float)distance), moments);
        }

        return mass;
    }

    void ProjectTriangle(int triangle, float4x4 localToAmbisonic, double4* scratch, out double solidAngle, out double distance)
    {
        float3x3 vertices = Triangles[triangle];

        PolygonProjection.ProjectTriangle(
            math.transform(localToAmbisonic, vertices.c0),
            math.transform(localToAmbisonic, vertices.c1),
            math.transform(localToAmbisonic, vertices.c2),
            (double4*)DirectionsX.GetUnsafeReadOnlyPtr(),
            (double4*)DirectionsY.GetUnsafeReadOnlyPtr(),
            (double4*)DirectionsZ.GetUnsafeReadOnlyPtr(),
            scratch,
            out solidAngle,
            out distance
        );
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

    public void Execute(int rowIdx)
    {
        double4* moments = stackalloc double4[PolygonProjection.BANDED_MOMENTS];
        double3 mass = SumChunks(rowIdx, moments);

        AmbiHandle handle = GeneratorHandles[rowIdx];
        int generatorIdx = GeneratorHandleToSoA[handle.Index];

        float* target = AmbisonicBuffer.Get(GeneratorOutputs, generatorIdx);

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            float* bandTarget = AmbisonicBuffer.GetBand(target, band);

            if (mass[band] > 0.0)
            {
                double4* bandMoments = BandedMoments.GetBand(moments, band);
                ShapeBand(generatorIdx, band, bandMoments, mass[band], bandTarget);
            }
            else
                AmbisonicBuffer.ClearBand(bandTarget);
        }
    }

    // returns the per band solid angle before propagation
    double3 SumChunks(int rowIdx, double4* moments)
    {
        BandedMoments.Clear(moments);
        double3 mass = 0.0;

        int firstChunk = ChunkOffsets[rowIdx];

        for (int chunk = firstChunk; chunk < firstChunk + ChunkCounts[rowIdx]; chunk++)
        {
            double4* chunkMoments = BandedMoments.GetReadOnly(ChunkMoments, chunk);

            for (int index = 0; index < PolygonProjection.BANDED_MOMENTS; index++)
                moments[index] += chunkMoments[index];

            mass += ChunkMasses[chunk];
        }

        return mass;
    }

    void ShapeBand(int generatorIdx, int band, double4* bandMoments, double bandMass, float* bandTarget)
    {
        int3 orders = GeneratorBandOrders[generatorIdx];
        float3 gains = GeneratorBandGains[generatorIdx];
        float3 spreads = GeneratorBandSpreads[generatorIdx];
        float horizontalSpread = GeneratorHorizontalSpreads[generatorIdx];

        float* distribution = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];
        Distribution(bandMoments, bandMass, SphericalHarmonics.ChannelCount(orders[band]), distribution);

        SphericalHarmonics.Shape(distribution, orders[band], gains[band], spreads[band], horizontalSpread, bandTarget);
    }

    // per unit solid angle: c00 == Y00 * mean propagation gain, identical energy scale to a point source
    void Distribution(double4* bandMoments, double bandMass, int channels, float* distribution)
    {
        double* harmonics = stackalloc double[HC.MAX_AMBISONIC_CHANNELS];
        double4* momentToHarmonic = (double4*)MomentToHarmonic.GetUnsafeReadOnlyPtr();

        PolygonProjection.Resolve(bandMoments, momentToHarmonic, channels, harmonics);

        double normalization = 1.0 / bandMass;

        for (int channel = 0; channel < channels; channel++)
            distribution[channel] = (float)(harmonics[channel] * normalization);
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
        NativeArray<Color> colors = new(VertexColorBands ? Source.vertexCount : 0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        Source.GetVertices(vertices);

        if (VertexColorBands)
            Source.GetColors(colors);

        for (int submesh = 0; submesh < Source.subMeshCount; submesh++)
        {
            SubMeshDescriptor descriptor = Source.GetSubMesh(submesh);

            if (descriptor.topology == MeshTopology.Triangles)
                CacheSubmesh(submesh, descriptor.indexCount, vertices, colors);
        }
    }

    void CacheSubmesh(int submesh, int indexCount, NativeArray<Vector3> vertices, NativeArray<Color> colors)
    {
        var indices = new NativeArray<int>(indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
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

    static float3 Bands(Color color) => new(color.r, color.g, color.b);
}
