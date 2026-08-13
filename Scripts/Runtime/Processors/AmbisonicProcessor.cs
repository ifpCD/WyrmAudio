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
        var calculateEqualizationJob  = new CalculateAmbisonicEqualizationJob
        {
            InputAmbisonicEQLow01s   = WyrmBaseSource.InputAmbisonicEQLow01s,
            InputAmbisonicEQMid01s    = WyrmBaseSource.InputAmbisonicEQMid01s,
            InputAmbisonicEQHigh01s    = WyrmBaseSource.InputAmbisonicEQHigh01s,

            TargetAmbisonicEQ01s      = WyrmBaseSource.TargetAmbisonicEQ01s,
        };
        JobHandle calculateEqualizationHandle = calculateEqualizationJob.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);

        // csharpier-ignore
        var shCoeffJob                = new GenerateDirectionalSHJob
        {
            VirtualPositions          = WyrmBaseSource.InputVirtualPositions,
            PathEQs                   = WyrmBaseSource.TargetAmbisonicEQ01s,
            ListenerPosition          = WyrmListener.ListenerPosition.Value,
            AmbisonicOrder            = SteamAudioSettings.Singleton.realTimeAmbisonicOrder,

            SHCoeffs                  = WyrmBaseSource.TargetSHCoefficients,
        };
        JobHandle shCoefficientHandle = shCoeffJob.Schedule(WyrmBaseSource.ActiveCount, 16, calculateEqualizationHandle);

        return shCoefficientHandle;
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
