using System;
using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public interface IWyrmSource
{
    int ActiveIndex { get; set; }
    WyrmMixerGroupManager Manager { get; set; }

    public bool isPlaying { get; }

    public Transform CachedTransform { get; }
    public Transform TrackedTransform { get; set; }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }

    public WyrmMixerGroupConfig Config { get; }

    public void Initialize(WyrmMixerGroupConfig config);

    public void Play(WyrmSoundBank clip, float? volume = null, Transform track = null);

    public void Deactivate();

    // MonoBehaviour
    public GameObject gameObject { get; }

    // AudioSource
    public AudioClip clip { get; set; }
    public bool loop { get; set; }
    public float volume { get; set; }
    public float pitch { get; set; }
    public float minDistance { get; set; }
    public float maxDistance { get; set; }
    public void Play();
    public void PlayOneShot(AudioClip clip);
    public void Stop();
}