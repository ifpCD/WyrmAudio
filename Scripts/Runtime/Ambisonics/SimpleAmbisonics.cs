using Unity.Scripting.LifecycleManagement;

public enum SimpleAmbisonicType : byte
{
    Directional,
    Positional,
}

[NoAutoStaticsCleanup]
internal sealed partial class SimpleAmbisonics : AmbiBase<SimpleAmbisonics>
{
    protected override int AllocatedCapacity => HC.MAX_AMBISONIC_CONTRIBUTORS;

    readonly WyrmAmbisonicGenerator _generator;

    internal SimpleAmbisonics(WyrmAmbisonicGenerator generator) => _generator = generator;

    internal void SyncType()
    {
        if (IsRegistered)
            Types[SoAIndex] = (byte)_generator.SimpleType;
    }
}
