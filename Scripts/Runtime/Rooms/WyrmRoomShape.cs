using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed partial class WyrmRoomShape : AmbiMonoBehaviour<WyrmRoomShape>, IEasyCollider
{
    protected override int AllocatedCapacity => 2000;

    [field: SerializeField]
    public BoxCollider BoxCollider { get; private set; }

    [field: SerializeField]
    public Transform BottomLeft { get; private set; }

    [field: SerializeField]
    public Transform TopRight { get; private set; }

    [HideInInspector]
    public EasyColliderState State { get; private set; } = new();

    public float4x4 WorldToLocal => math.inverse(transform.localToWorldMatrix);
    public Vector3 Extents => BoxCollider.size * .5f;

    void OnEnable() => Register();

    void OnDisable() => Deregister();

    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    [Range(8, 32)]
    public int sampleCount = 32;

    [HideInInspector]
    public List<Vector3> _haltonBuffer = new(32);

    public Color OutlineColor => WyrmColor.FaintYellow;
    public Color OutlineSelected => WyrmColor.Yellow;

    public Color VolumeSelected => new(0, .1f, 1f, .05f);

    public Color VolumeColor { get; set; } = Color.clear;

#if UNITY_EDITOR

    GUIStyle labelStyle;

    void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity());

        Handles.Label(transform.position, gameObject.name, labelStyle);

        this.DrawVolumeGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        this.DrawVolumeGizmo(true);

        if (_haltonBuffer == null)
            return;

        Gizmos.color = Color.grey;

        var matrix = transform.localToWorldMatrix;

        foreach (var p in _haltonBuffer)
            Gizmos.DrawSphere(matrix.MultiplyPoint3x4(p), .04f);
    }

    void OnValidate()
    {
        BoxCollider = this.EnsureReference(BoxCollider);

        this.ValidateCollider();

        HaltonSequence.GenerateBoxVolumeSamples(BoxCollider, sampleCount, _haltonBuffer);
    }

    void Update()
    {
        if (WyrmListener.CompletelyInactive)
            return;

        var ListenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier.Value;
        if (ListenerRoomIdentifier == -1)
            return;

        VolumeColor = ListenerRoomIdentifier == RoomIdentifier ? new Color(0, .5f, .5f, .5f) : Color.clear;
    }

#endif
}
