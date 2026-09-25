using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// User written
[AmbiSynchronizable]
[RequireComponent(typeof(BoxCollider))]
[NoAutoStaticsCleanup]
public partial class AmbiNode : AmbiMonoBehaviour<AmbiNode>
{
    protected override int AllocatedCapacity => 2000;

    // MANAGED SECTION
    public BoxCollider BoxCollider { get; private set; }

    public Vector3 Position
    {
        get => Positions.GetOrDefault(SoAIndex);
    }

    public Quaternion Rotation
    {
        get => Quaternions.GetOrDefault(SoAIndex);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    public bool IsInRoom => IsInRooms.GetOrDefault(SoAIndex).ToBool();
}
