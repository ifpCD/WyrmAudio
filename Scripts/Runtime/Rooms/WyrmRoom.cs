using UnityEngine;

public class WyrmAudioRoom : MonoBehaviour, IWyrmRoom
{
    public int RoomID { get; }
    
    public BoxCollider boxCollider;
}