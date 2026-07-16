using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public partial class WyrmRoomShape : EasyCollider
{
    GraphManager _owner;
    private int _nativeIndex = -1;

    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    [Range(8, 32)]
    public int sampleCount = 32;

    [HideInInspector]
    public List<Vector3> samplesContainer;

    protected override Color OutlineColor { get; set; } = Color.cyan;
    protected override Color VolumeColor { get; set; } = new(0, 0, 0, 0f);

    protected override Color OutlineSelected { get; set; } = Color.green;
    protected override Color VolumeSelected { get; set; } = new(0, 0.1f, 1, 0.05f);

    internal void InformOfRegistration(GraphManager owner, int myIndex)
    {
        _owner = owner;
        _nativeIndex = myIndex;
        Populate();
    }

    public void Populate()
    {
        _owner.ShapeWorldToLocal[_nativeIndex] = math.inverse(transform.localToWorldMatrix);
        _owner.ShapeExtents[_nativeIndex] = BoxCollider.size * 0.5f;

        _owner.ShapeRoomIdentifier[_nativeIndex] = RoomIdentifier;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        samplesContainer ??= new List<Vector3>();

        if (BoxCollider != null)
            Sampling.GenerateBoxVolumeSamples(BoxCollider, sampleCount, samplesContainer);
    }

    protected override void OnDrawGizmosSelected()
    {
        if (samplesContainer == null || samplesContainer.Count == 0)
            return;

        Gizmos.color = Color.green;

        foreach (Vector3 point in samplesContainer)
        {
            Gizmos.DrawSphere(point, 0.04f);
        }
        base.OnDrawGizmos();
    }

    void Update()
    {
        var graphProvider = GraphManager.Instance;
        if (graphProvider != null)
        {
            if (graphProvider.ListenerRoomIdentifier.Value == RoomIdentifier)
            {
                VolumeColor = new(0, 0.5f, 0.5f, 0.5f);
                return;
            }
        }

        VolumeColor = new(0, 0, 0, 0);
    }
#endif
}
