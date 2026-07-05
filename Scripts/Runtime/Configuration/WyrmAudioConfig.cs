using UnityEngine;

public class WyrmAudioConfig : ScriptableObject
{
    public WyrmAudioConfig Instance { get; internal set; }

    public GameObject selectedPoolManager;
    public LayerMask roomLayer;
}
