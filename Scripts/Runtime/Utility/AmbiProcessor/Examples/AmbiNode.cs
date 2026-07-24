using System;
using UnityEngine;

// User written
[AmbiSynchronizable]
[RequireComponent(typeof(BoxCollider))]
public partial class AmbiNode : AmbiComponent<AmbiNode>
{
    // MANAGED SECTION
    public BoxCollider BoxCollider { get; private set; }

    public Vector3 Position
    {
        get => Positions.GetOrDefault(EnabledIndex);
    }

    public Quaternion Rotation
    {
        get => Quaternions.GetOrDefault(EnabledIndex);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    public bool IsInRoom
    {
        get => IsInRooms.GetOrDefault(EnabledIndex).ToBool();
    }

    [AmbiCallback(AmbiManagedHookType.PreBatchUpdate)]
    void PreBatchUpdate() { }

    [AmbiCallback(AmbiManagedHookType.PostBatchUpdate)]
    void PostBatchUpdate() { }
}
