using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

internal static class SourceProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive)
            return dependency;

        // csharpier-ignore
        var loadAmbisonicOutputsToSourcesHandle = new LoadAmbisonicOutputsToSourcesJob
        {
            SourceGeneratorHandles         = WyrmBaseSource.AmbisonicGeneratorHandles,
            SourceBandGains                = WyrmBaseSource.CurrentAmbisonicEQ01s,

            GeneratorHandleToSoA           = WyrmAmbisonicGenerator.HandleToSoA,
            GeneratorVersions              = WyrmAmbisonicGenerator.HandleVersions,
            GeneratorBandOrders            = WyrmAmbisonicGenerator.BandOrders,
            GeneratorOutputs               = WyrmAmbisonicGenerator.Outputs,

            SourceOutputs                  = WyrmBaseSource.TargetAmbisonicOutputs,
            SourceBandOrders               = WyrmBaseSource.TargetAmbisonicOrders,
        }.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);

        // csharpier-ignore
        var applyHandle = new ApplyMaskOcclusionToSourceJob
        {
            SourceMaskHandles    = WyrmBaseSource.OcclusionMaskHandles,
            UseOcclusions        = WyrmBaseSource.UseOcclusions,

            MaskHandleToSoA      = WyrmOcclusionMask.HandleToSoA,
            MaskVersions         = WyrmOcclusionMask.HandleVersions,
            MaskTargetOcclusions = WyrmOcclusionMask.TargetOcclusionValue01s,

            TargetOcclusion01    = WyrmBaseSource.TargetOcclusion01,
        }.Schedule(WyrmBaseSource.ActiveCount, 32, dependency);

        JobHandle finalizerHandle = JobHandle.CombineDependencies(loadAmbisonicOutputsToSourcesHandle, applyHandle);

        return finalizerHandle;
    }
}
