using System;
using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public interface IWyrmSource
{
    WyrmMixerGroupConfig Config { get; }

    void Initialize(WyrmMixerGroupProcessor manager);

    Transform TrackedTransform { get; set; }

    // future
    void Play(AudioClip clip, Transform track = null, float? volume = null);
    void Play(AbstractWyrmBank clip, Transform track = null, float? volume = null);

    void Return();

    bool IsBorrowed { get; set; }
    int ActiveIndex { get; set; }
    WyrmMixerGroupProcessor Manager { get; }

    Transform CachedTransform { get; }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }


    public float TargetVolume { get; set; }

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
    void PlayOneShot(AbstractWyrmBank clip);
    void Stop();
    bool isPlaying { get; }
}