using System;
using NUnit.Framework.Constraints;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public partial class WyrmPortal : AmbiComponent<WyrmPortal>, IEasyCollider
{
    protected override int MaximumCapacity => 2000;

    [field: SerializeField]
    public BoxCollider BoxCollider { get; private set; }

    [field: SerializeField]
    public Transform BottomLeft { get; private set; }

    [field: SerializeField]
    public Transform TopRight { get; private set; }

    [HideInInspector]
    public EasyColliderState State { get; private set; } = new();

    [Header("Relation")]
    [field: SerializeField]
    public WyrmRoomShape RoomA { get; set; }

    [field: SerializeField]
    public WyrmRoomShape RoomB { get; set; }

    public Vector3 Extents => BoxCollider.size * .5f;

    void OnEnable() => Register();

    void OnDisable() => Deregister();

    public float _openness = 1f;

    [Range(0, 1)]
    public float Openness
    {
        get => _openness;
        set
        {
            _openness = value;
            PortalOpenness.SetIfEnabled(value, NativeIndex);
        }
    }

#if UNITY_EDITOR
    public Color OutlineColor => GizmoColors.FaintCyan;
    public Color OutlineSelected => GizmoColors.Cyan;

    public Color VolumeSelected => new(0, .1f, 1f, .05f);

    public Color VolumeColor { get; set; } = GizmoColors.None;

    GUIStyle labelStyle;

    void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity() * .2f);

        Handles.Label(transform.position, gameObject.name, labelStyle);

        this.DrawVolumeGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        this.DrawVolumeGizmo(true);
    }

    void OnValidate()
    {
        if (!BoxCollider)
            BoxCollider = GetComponent<BoxCollider>();

        BoxCollider.enabled = false;
        this.ValidateCollider();
    }
#endif
}
