using UnityEngine;

[DisallowMultipleComponent]
public partial class WyrmPortal : EasyCollider
{
    [Header("Relation")]
    [field: SerializeField]
    public WyrmRoom RoomA { get; set; }

    [field: SerializeField]
    public WyrmRoom RoomB { get; set; }

    public int[] RoomIdentifierConnections => new int[]
    {
        RoomA != null ? RoomA.RoomIdentifier : -1,
        RoomB != null ? RoomB.RoomIdentifier : -1
    };

    [Header("State")]
    [Range(0f, 1f)]
    public float Openness = 1f;

    protected override Color OutlineColor => Color.cyan;
    protected override Color VolumeColor => new(0, 0, 0, 0f);

    protected override Color OutlineSelected => new(0, 0.1f, 1, 1f);
    protected override Color VolumeSelected => new(0, 0.1f, 1, 0.05f);
}