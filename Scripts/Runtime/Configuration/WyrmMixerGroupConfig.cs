using UnityEngine;
using UnityEngine.Audio;

public class WyrmMixerGroupConfig
{
    public AudioMixerGroup targetMixerGroup;
    public GameObject WyrmAudioSourcePrefab;

    [Range(1, 500)]
    public int initialSize = 10;

    [Range(2, 1000)]
    public int maxSize = 30;

    public bool isNonSpatial;
}
