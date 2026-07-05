using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class WyrmMixerGroupConfig
{
    public AudioMixerGroup targetMixerGroup;
    public GameObject WyrmAudioSourcePrefab;
    public int initialSize = 10;
    public int maxSize = 30;
    public bool warnOverflow;
}
