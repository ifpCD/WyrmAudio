using System;
using UnityEngine;

// User written
[RequireComponent(typeof(BoxCollider))]
public partial class AmbiNode : MonoBehaviour
{
    // parallel jobs, unsafe pointer calls into C++, etc here
    internal static void BatchUpdate() { }

    // MANAGED SECTION
    public BoxCollider BoxCollider { get; private set; }

    public Vector3 Position
    {
        get => Positions.GetOrDefault(_index);
    }

    public Quaternion Rotation
    {
        get => Quaternions.GetOrDefault(_index);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    public bool IsInRoom
    {
        get => Convert.ToBoolean(IsInRooms.GetOrDefault(_index));
    }

    internal void PreBatchUpdate() { }

    internal void PostBatchUpdate() { }
}
