using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed partial class MeshAmbisonics
{
    const int TRIANGLES_PER_CHUNK = 64;

    const int INITIAL_TRIANGLE_CAPACITY = 1024;
    const int INITIAL_CHUNK_CAPACITY = 64;

    // the lists below can reallocate on registration, so it waits for the last scheduled projection
    internal static JobHandle Projection;

    // local-space triangles of every row, packed back to back
    internal static NativeList<float3x3> Triangles;
    internal static NativeList<float3> TriangleBands;
    internal static NativeArray<int> TriangleOffsets;
    internal static NativeArray<int> TriangleCounts;

    // fixed-size work units (row, first triangle, count) so one dense mesh spreads over every worker
    internal static NativeList<int3> Chunks;
    internal static NativeArray<int> ChunkOffsets;
    internal static NativeArray<int> ChunkCounts;
    internal static NativeList<double4> ChunkMoments; // one BandedMoments block per chunk
    internal static NativeList<double3> ChunkMasses; // per band solid angle, before propagation

    static bool _chunksStale;

    // csharpier-ignore
    static void AllocateGeometry(int capacity)
    {
        Triangles       = new(INITIAL_TRIANGLE_CAPACITY, Allocator.Persistent);
        TriangleBands   = new(INITIAL_TRIANGLE_CAPACITY, Allocator.Persistent);
        TriangleOffsets = new(capacity, Allocator.Persistent);
        TriangleCounts  = new(capacity, Allocator.Persistent);

        Chunks          = new(INITIAL_CHUNK_CAPACITY, Allocator.Persistent);
        ChunkOffsets    = new(capacity, Allocator.Persistent);
        ChunkCounts     = new(capacity, Allocator.Persistent);
        ChunkMoments    = BandedMoments.Allocate(INITIAL_CHUNK_CAPACITY, Allocator.Persistent);
        ChunkMasses     = new(INITIAL_CHUNK_CAPACITY, Allocator.Persistent);
    }

    static void DeallocateGeometry()
    {
        Triangles.Dispose();
        TriangleBands.Dispose();
        TriangleOffsets.TryDispose();
        TriangleCounts.TryDispose();

        Chunks.Dispose();
        ChunkOffsets.TryDispose();
        ChunkCounts.TryDispose();
        ChunkMoments.Dispose();
        ChunkMasses.Dispose();

        _chunksStale = false;
    }

    // Registration-time only
    void CacheTriangles(Mesh mesh, bool vertexColorBands)
    {
        using Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh);
        int offset = Triangles.Length;

        bool hasVertexColors = vertexColorBands && mesh.HasVertexAttribute(VertexAttribute.Color);

        new CacheMeshTrianglesJob
        {
            Source = meshData[0],
            VertexColorBands = hasVertexColors,
            Triangles = Triangles,
            TriangleBands = TriangleBands,
        }.Run();

        TriangleOffsets[SoAIndex] = offset;
        TriangleCounts[SoAIndex] = Triangles.Length - offset;
        _chunksStale = true;
    }

    // csharpier-ignore
    static void RemoveTriangles(int removedIndex, int lastIndex)
    {
        int start = TriangleOffsets[removedIndex];
        int count = TriangleCounts[removedIndex];

        Triangles.RemoveRange(start, count);
        TriangleBands.RemoveRange(start, count);

        for (int row = 0; row <= lastIndex; row++)
        {
            if (TriangleOffsets[row] > start)
                TriangleOffsets[row] -= count;
        }

        TriangleOffsets[removedIndex] = TriangleOffsets[lastIndex];
        TriangleCounts[removedIndex]  = TriangleCounts[lastIndex];
        _chunksStale = true;
    }

    // Main thread, before the frame is scheduled.
    internal static void RefreshChunks()
    {
        if (!_chunksStale)
            return;

        _chunksStale = false;
        Chunks.Clear();

        for (int row = 0; row < ActiveCount; row++)
        {
            int offset = TriangleOffsets[row];
            int count = TriangleCounts[row];
            ChunkOffsets[row] = Chunks.Length;

            for (int first = 0; first < count; first += TRIANGLES_PER_CHUNK)
                Chunks.Add(new int3(row, offset + first, math.min(TRIANGLES_PER_CHUNK, count - first)));

            ChunkCounts[row] = Chunks.Length - ChunkOffsets[row];
        }

        BandedMoments.Resize(ChunkMoments, Chunks.Length);
        ChunkMasses.ResizeUninitialized(Chunks.Length);
    }
}
