using SteamAudio;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

internal static class AmbisonicProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive || WyrmAudioManager.Listener == null)
            return dependency;

        var simpleAmbisonics = new SimpleAmbisonicsGeneration
        {
            ListenerPosition = WyrmListener.ListenerPosition.Value,
            Types = WyrmAmbisonicGenerator.Types,

            VirtualDirections = SimpleAmbisonics.VirtualDirections,
            VirtualPositions = SimpleAmbisonics.VirtualPositions,

            VerticalBlurs = SimpleAmbisonics.VerticalBlurs,
            HorizontalBlurs = SimpleAmbisonics.HorizontalBlurs,

            AmbisonicOrder = 3,
            EQVolume01s = WyrmAmbisonicGenerator.EQVolume01s,

            AmbisonicOutputsBuffer = WyrmAmbisonicGenerator.TargetAmbisonicOutputs,
        };
        JobHandle simpleAmbisonicsGenerationJob = simpleAmbisonics.Schedule(SimpleAmbisonics.ActiveCount, 16, dependency);

        var loadAmbisonicOutputsToSources = new LoadAmbisonicOutputsToSources
        {
            SourceToAmbisonicGeneratorHandles = WyrmBaseSource.AmbisonicGeneratorHandles,
            AmbisonicGeneratorBuffers = WyrmAmbisonicGenerator.CurrentAmbisonicOutputs,
            SourceAmbisonicBuffers = WyrmBaseSource.TargetAmbisonicOutputs,
            GeneratorHandleToSoAIndex = WyrmAmbisonicGenerator.HandleToSoA,
            GeneratorVersions = WyrmAmbisonicGenerator.HandleVersions,
        };
        JobHandle LoadAmbisonicOutputsToSourcesJob = loadAmbisonicOutputsToSources.Schedule(
            WyrmBaseSource.ActiveCount,
            16,
            simpleAmbisonicsGenerationJob
        );

        return LoadAmbisonicOutputsToSourcesJob;
    }
}

[BurstCompile]
internal struct CalculateAmbisonicEqualizationJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQLow01s;

    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQMid01s;

    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQHigh01s;

    [WriteOnly]
    public NativeArray<float3> TargetAmbisonicEQ01s;

    public void Execute(int i)
    {
        TargetAmbisonicEQ01s[i] = new(InputAmbisonicEQLow01s[i], InputAmbisonicEQMid01s[i], InputAmbisonicEQHigh01s[i]);
    }
}
