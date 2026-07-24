using System;
using SaintsField.Playa;
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
        get => Positions.GetOrDefault(NativeIndex);
    }

    public Quaternion Rotation
    {
        get => Quaternions.GetOrDefault(NativeIndex);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    [ShowInInspector]
    public bool IsInRoom => IsInRooms.GetOrDefault(NativeIndex).ToBool();
}
