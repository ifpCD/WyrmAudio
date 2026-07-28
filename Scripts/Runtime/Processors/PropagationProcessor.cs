using Unity.Jobs;

internal static class PropagationProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        return new JobHandle();
    }
}
