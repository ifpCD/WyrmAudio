using UnityEngine;

[DisallowMultipleComponent]
public partial class WyrmRoom : EasyCollider
{
    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    protected override Color OutlineColor => Color.cyan;
    protected override Color VolumeColor => new(0, 0, 0, 0f);

    protected override Color OutlineSelected => Color.green;
    protected override Color VolumeSelected => new(0, 0.1f, 1, 0.05f);
}