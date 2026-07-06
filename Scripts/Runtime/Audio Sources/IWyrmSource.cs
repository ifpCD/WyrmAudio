using System;
using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public interface IWyrmSource
{
    WyrmMixerGroupConfig Config { get; }

    void Initialize(WyrmMixerGroupManager manager);

    Transform TrackedTransform { get; set; }

    void Play(WyrmSoundBank clip, float? volume = null, Transform track = null);

    int ActiveIndex { get; set; }
    WyrmMixerGroupManager Manager { get; }

    Transform CachedTransform { get; }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }

    void Deactivate();

    // MonoBehaviour
    GameObject gameObject { get; }

    // AudioSource
    AudioSource ASource { get; set; }
    AudioClip clip { get; set; }
    bool loop { get; set; }
    float volume { get; set; }
    float pitch { get; set; }
    float minDistance { get; set; }
    float maxDistance { get; set; }
    void Play();
    void PlayOneShot(AudioClip clip);
    void Stop();
    bool isPlaying { get; }
}