using System;
using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public interface IWyrmSource
{
    AudioSource ASource { get; set; }

    int ActiveIndex { get; set; }
    WyrmMixerGroupManager Manager { get; }

    bool isPlaying { get; }

    Transform CachedTransform { get; }
    Transform TrackedTransform { get; set; }

    Vector3 CachedPosition { get; set; }
    Quaternion CachedRotation { get; set; }

    WyrmMixerGroupConfig Config { get; }

    void Initialize(WyrmMixerGroupManager manager);

    void Play(WyrmSoundBank clip, float? volume = null, Transform track = null);

    void Deactivate();

    // MonoBehaviour
    GameObject gameObject { get; }

    // AudioSource
    AudioClip clip { get; set; }
    bool loop { get; set; }
    float volume { get; set; }
    float pitch { get; set; }
    float minDistance { get; set; }
    float maxDistance { get; set; }
    void Play();
    void PlayOneShot(AudioClip clip);
    void Stop();
}