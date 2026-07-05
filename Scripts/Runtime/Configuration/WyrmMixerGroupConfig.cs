using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Wyrm Audio/Mixer Group Config")]
public class WyrmMixerGroupConfig : ScriptableObject
{
    [Header("Setup")]
    public AudioMixerGroup targetMixerGroup;
    public GameObject WyrmAudioSourcePrefab;

    [Header("Pooling")]
    [Range(1, 500)]
    public int initialSize = 5;

    [Range(2, 1000)]
    public int maxSize = 10;

    [Header("Settings")]
    public bool isNonSpatial;
    public bool isRoomMixed;
}
