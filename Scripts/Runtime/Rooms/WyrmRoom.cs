using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public partial class WyrmRoomShape : EasyCollider
{
    GraphManager _owner;
    private int _nativeIndex = -1;

    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    protected override Color OutlineColor { get; set; } = Color.cyan;
    protected override Color VolumeColor { get; set; } = new(0, 0, 0, 0f);

    protected override Color OutlineSelected { get; set; } = Color.green;
    protected override Color VolumeSelected { get; set; } = new(0, 0.1f, 1, 0.05f);

    public PartialRoomShapeData ToNative()
    {
        return new PartialRoomShapeData
        {
            roomIdentifier = RoomIdentifier,

            extents = BoxCollider.size * 0.5f,

            worldToLocal = math.inverse(transform.localToWorldMatrix)
        };
    }

    internal void InformOfRegistration(GraphManager owner, int myIndex)
    {
        _owner = owner;
        _nativeIndex = myIndex;
        Populate();
    }

    public void Populate()
    {
        _owner.ShapeRoomIdentifier[_nativeIndex] = RoomIdentifier;

        _owner.ShapeWorldToLocal[_nativeIndex] = math.inverse(transform.localToWorldMatrix);
        _owner.ShapeExtents[_nativeIndex] = BoxCollider.size * 0.5f;
    }

#if UNITY_EDITOR
    void Update()
    {
        var graphProvider = GraphManager.Instance;
        if (graphProvider != null)
        {
            if (graphProvider.ListenerRoomIdentifier.Value == RoomIdentifier)
            {
                VolumeColor = new(0, 0.5f, 0.5f, 0.5f);
            }
            else VolumeColor = new(0, 0, 0, 0);
        }
    }
#endif
}