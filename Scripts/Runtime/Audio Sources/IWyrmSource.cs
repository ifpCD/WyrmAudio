using System;
using UnityEngine;
#pragma warning disable IDE1006 // gameObject MonoBehaviour

public interface IWyrmSource
{
    public bool IsPlaying { get; }

    public Transform BaseTransform { get; }
    public Transform TrackedTransform { get; set; }

    public void Play();
    public void Play(WyrmAudioClip clip, float? playbackLengthOverride = null);
    public void PlayOneShot(AudioClip clip);

    public void Deactivate();

    // MonoBehaviour
    public GameObject gameObject { get; }

    // AudioSource
    public AudioClip clip { get; set; }
    public float volume { get; set; }
    public float pitch { get; set; }
}