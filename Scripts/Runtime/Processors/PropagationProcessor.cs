using Unity.Jobs;

internal class PropagationProcessor : IPropagationProcessor
{
    public JobHandle SchedulePropagation()
    {
        throw new System.NotImplementedException();
    }
}

internal interface IPropagationProcessor
{
    JobHandle SchedulePropagation();
}