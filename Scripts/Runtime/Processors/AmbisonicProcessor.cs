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
            return new ClearSourceAmbisonicBuffers
            {
                Length                     = WyrmBaseSource.ActiveCount * HC.AMBISONIC_BUFFER_LENGTH,
                SourceAmbisonicBuffers     = WyrmBaseSource.TargetAmbisonicOutputs,
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
                GeneratorBandGains         = WyrmAmbisonicGenerator.BandGains,
                GeneratorBandSpreads       = WyrmAmbisonicGenerator.BandSpreads,
                GeneratorHorizontalSpreads = WyrmAmbisonicGenerator.HorizontalSpreads,

                GeneratorOutputs           = WyrmAmbisonicGenerator.Outputs,
            }.ScheduleReadOnly(SimpleAmbisonics.Transforms, 16, dependency);
        }

        if (!MeshAmbisonics.CompletelyInactive)
            generation = ScheduleMeshProjection(listenerPosition, dependency, generation);

        var loadAmbisonicOutputsToSources = new LoadAmbisonicOutputsToSources
        {
            SourceToAmbisonicGeneratorHandles = WyrmBaseSource.AmbisonicGeneratorHandles,
            AmbisonicGeneratorBuffers = WyrmAmbisonicGenerator.Outputs,
            SourceAmbisonicBuffers = WyrmBaseSource.TargetAmbisonicOutputs,
            GeneratorHandleToSoAIndex = WyrmAmbisonicGenerator.HandleToSoA,
            GeneratorVersions = WyrmAmbisonicGenerator.HandleVersions,
        };
        JobHandle LoadAmbisonicOutputsToSourcesJob = loadAmbisonicOutputsToSources.Schedule(
            WyrmBaseSource.ActiveCount,
            16,
            generation
        );

        return LoadAmbisonicOutputsToSourcesJob;
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

        return MeshAmbisonics.Projection = new ReduceMeshMomentsJob
        {
            ChunkOffsets               = MeshAmbisonics.ChunkOffsets,
            ChunkCounts                = MeshAmbisonics.ChunkCounts,
            ChunkMoments               = MeshAmbisonics.ChunkMoments.AsArray(),
            MomentToHarmonic           = MeshAmbisonics.MomentToHarmonic,
            GeneratorHandles           = MeshAmbisonics.GeneratorHandles,

            GeneratorHandleToSoA       = WyrmAmbisonicGenerator.HandleToSoA,
            GeneratorBandGains         = WyrmAmbisonicGenerator.BandGains,
            GeneratorBandSpreads       = WyrmAmbisonicGenerator.BandSpreads,
            GeneratorHorizontalSpreads = WyrmAmbisonicGenerator.HorizontalSpreads,

            GeneratorOutputs           = WyrmAmbisonicGenerator.Outputs,
        }.Schedule(MeshAmbisonics.ActiveCount, 1, JobHandle.CombineDependencies(chunks, outputsDependency));
    }
}
