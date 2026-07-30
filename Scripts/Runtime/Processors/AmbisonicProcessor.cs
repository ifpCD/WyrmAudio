using SteamAudio;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

internal static class AmbisonicProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive || WyrmListener.CompletelyInactive)
            return dependency;

        // csharpier-ignore
        var shCoeffJob = new GenerateSHCoefficientsJob
        {
            VirtualPositions = WyrmBaseSource.InputVirtualPositions,
            VerticalWidths   = WyrmBaseSource.InputVerticalWidths,
            HorizontalWidths = WyrmBaseSource.InputHorizontalWidths,
            DirectionalGains = WyrmBaseSource.InputDirectionalGains,
            AmbientGains     = WyrmBaseSource.InputAmbientGains,

            ListenerPosition = WyrmListener.ListenerPosition,

            AmbisonicOrder = SteamAudioSettings.Singleton.realTimeAmbisonicOrder,

            SHCoeffs         = WyrmBaseSource.TargetSHCoefficients,
        };
        JobHandle shCoefficientHandle = shCoeffJob.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);

        var calculateEqualizationJob = new CalculateAmbisonicEqualizationJob
        {
            InputAmbisonicEQHigh01s = WyrmBaseSource.InputAmbisonicEQLow01s,
            InputAmbisonicEQMid01s = WyrmBaseSource.InputAmbisonicEQMid01s,
            InputAmbisonicEQLow01s = WyrmBaseSource.InputAmbisonicEQHigh01s,

            TargetAmbisonicEQ01s = WyrmBaseSource.TargetAmbisonicEQ01s,
        };
        JobHandle calculateEqualizationHandle = calculateEqualizationJob.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);

        return JobHandle.CombineDependencies(shCoefficientHandle, calculateEqualizationHandle);
    }
}

[BurstCompile]
internal struct CalculateAmbisonicEqualizationJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQHigh01s;

    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQMid01s;

    [ReadOnly]
    public NativeArray<float> InputAmbisonicEQLow01s;

    [WriteOnly]
    public NativeArray<float3> TargetAmbisonicEQ01s;

    public void Execute(int i)
    {
        TargetAmbisonicEQ01s[i] = new(InputAmbisonicEQHigh01s[i], InputAmbisonicEQMid01s[i], InputAmbisonicEQLow01s[i]);
    }
}
