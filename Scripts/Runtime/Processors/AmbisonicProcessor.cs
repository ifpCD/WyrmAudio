using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

internal static class AmbisonicProcessor
{
    // csharpier-ignore
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive || WyrmAudioManager.Listener == null)
            return dependency;

        if (WyrmAmbisonicGenerator.CompletelyInactive)
        {
            return new ClearSourceAmbisonicsJob
            {
                SourceCount                = WyrmBaseSource.ActiveCount,

                SourceOutputs              = WyrmBaseSource.TargetAmbisonicOutputs,
                SourceBandOrders           = WyrmBaseSource.TargetAmbisonicOrders,
            }.Schedule(dependency);
        }

        float3 listenerPosition = WyrmListener.ListenerPosition.Value;
        JobHandle generation = dependency;

        if (!SimpleAmbisonics.CompletelyInactive)
        {
            generation = new EncodeSimpleAmbisonicsJob
            {
                ListenerPosition           = listenerPosition,
                Types                      = SimpleAmbisonics.Types,
                GeneratorHandles           = SimpleAmbisonics.GeneratorHandles,

                GeneratorHandleToSoA       = WyrmAmbisonicGenerator.HandleToSoA,
                GeneratorBandOrders        = WyrmAmbisonicGenerator.BandOrders,
                GeneratorBandGains         = WyrmAmbisonicGenerator.BandGains,
                GeneratorBandSpreads       = WyrmAmbisonicGenerator.BandSpreads,
                GeneratorHorizontalSpreads = WyrmAmbisonicGenerator.HorizontalSpreads,

                GeneratorOutputs           = WyrmAmbisonicGenerator.Outputs,
            }.ScheduleReadOnly(SimpleAmbisonics.Transforms, 16, dependency);
        }

        if (!MeshAmbisonics.CompletelyInactive)
            generation = ScheduleMeshProjection(listenerPosition, dependency, generation);

        return generation;
    }

    // Projection overlaps the simple encode; only the reduce shares the generator outputs with it.
    // csharpier-ignore
    static JobHandle ScheduleMeshProjection(float3 listenerPosition, JobHandle dependency, JobHandle outputsDependency)
    {
        MeshAmbisonics.RefreshChunks();

        JobHandle targets = new ReadMeshTargetsJob
        {
            ListenerPosition           = listenerPosition,

            LocalToAmbisonic           = MeshAmbisonics.LocalToAmbisonic,
        }.ScheduleReadOnly(MeshAmbisonics.Targets, 8, dependency);

        JobHandle chunks = new ProjectMeshChunksJob
        {
            Chunks                     = MeshAmbisonics.Chunks.AsArray(),
            LocalToAmbisonic           = MeshAmbisonics.LocalToAmbisonic,
            Triangles                  = MeshAmbisonics.Triangles.AsArray(),
            TriangleBands              = MeshAmbisonics.TriangleBands.AsArray(),
            DirectionsX                = MeshAmbisonics.MomentDirectionsX,
            DirectionsY                = MeshAmbisonics.MomentDirectionsY,
            DirectionsZ                = MeshAmbisonics.MomentDirectionsZ,

            ChunkMoments               = MeshAmbisonics.ChunkMoments.AsArray(),
        }.Schedule(MeshAmbisonics.Chunks.Length, 1, targets);

        JobHandle reduceMeshMoments = new ReduceMeshMomentsJob
        {
            ChunkOffsets               = MeshAmbisonics.ChunkOffsets,
            ChunkCounts                = MeshAmbisonics.ChunkCounts,
            ChunkMoments               = MeshAmbisonics.ChunkMoments.AsArray(),
            MomentToHarmonic           = MeshAmbisonics.MomentToHarmonic,
            GeneratorHandles           = MeshAmbisonics.GeneratorHandles,

            GeneratorHandleToSoA       = WyrmAmbisonicGenerator.HandleToSoA,
            GeneratorBandOrders        = WyrmAmbisonicGenerator.BandOrders,
            GeneratorBandGains         = WyrmAmbisonicGenerator.BandGains,
            GeneratorBandSpreads       = WyrmAmbisonicGenerator.BandSpreads,
            GeneratorHorizontalSpreads = WyrmAmbisonicGenerator.HorizontalSpreads,

            GeneratorOutputs           = WyrmAmbisonicGenerator.Outputs,
        }.Schedule(MeshAmbisonics.ActiveCount, 1, JobHandle.CombineDependencies(chunks, outputsDependency));

        MeshAmbisonics.Projection = reduceMeshMoments;

        return reduceMeshMoments;
    }
}
