using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public partial class WyrmRoom : MonoBehaviour
{
    [field: SerializeField, Tooltip("Unique ID for this room. Used for graph mapping.")]
    public int RoomIdentifier { get; internal set; }

    public BoxCollider BoxCollider { get; private set; }

    private void Awake()
    {
        BoxCollider = GetComponent<BoxCollider>();
        // If we are fully relying on our Native math bounds, we don't need the physics engine tracking this.
        if (Application.isPlaying) BoxCollider.enabled = false; 
    }
}