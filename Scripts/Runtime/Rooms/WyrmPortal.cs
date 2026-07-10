using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public partial class WyrmPortal : MonoBehaviour
{
    [Header("Connections")]
    [SerializeField] private WyrmRoom roomA;
    [SerializeField] private WyrmRoom roomB;

    public WyrmRoom RoomA => roomA;
    public WyrmRoom RoomB => roomB;

    public int[] RoomIdentifierConnections => new int[] 
    { 
        roomA != null ? roomA.RoomIdentifier : -1, 
        roomB != null ? roomB.RoomIdentifier : -1 
    };

    [Header("State")]
    [Range(0f, 1f)]
    public float Openness = 1f;

    public BoxCollider BoxCollider { get; private set; }

    private void Awake()
    {
        BoxCollider = GetComponent<BoxCollider>();
        if (Application.isPlaying) BoxCollider.enabled = false;
    }
}