using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Wyrm Audio/Mixer Group Config")]
public class WyrmMixerGroupConfig : ScriptableObject
{
    public AudioMixerGroup targetMixerGroup;
    public GameObject WyrmAudioSourcePrefab;

    [Range(1, 500)]
    public int initialSize = 10;

    [Range(2, 1000)]
    public int maxSize = 30;

    public bool isNonSpatial;
}
