using Unity.Scripting.LifecycleManagement;

[NoAutoStaticsCleanup]
internal sealed partial class MeshAmbisonics : AmbiBase<MeshAmbisonics>
{
    protected override int AllocatedCapacity => 256;

    readonly WyrmAmbisonicGenerator _generator;

    internal MeshAmbisonics(WyrmAmbisonicGenerator generator) => _generator = generator;
}
