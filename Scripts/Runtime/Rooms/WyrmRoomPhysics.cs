using UnityEngine;

public class WyrmAudioRoomPhysics : MonoBehaviour, IWyrmRoom
{
    public int RoomID { get; }
    public Collider Collider;
}