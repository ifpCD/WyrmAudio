using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class AudioGroupPoolingConfig : ScriptableObject
{
    public AudioMixerGroup mixerGroup;
    public int initialSize = 10;
    public int maxSize = 30;
    public IPooledAudioSource pooledAudioSourceType;
}
