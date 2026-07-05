using System;
using UnityEngine;
#pragma warning disable IDE1006 // gameObject MonoBehaviour

public interface IPooledAudioSource
{
    public bool IsPlaying { get; }
    public bool IsOneShot { get; }

    public Transform TrackedTransform { get; set; }
    public int PlayVersion { get; set; }

    public void Play();
    public void PlayOneShot(AudioClip clip);

    public void Stop();

    // MonoBehaviour
    public GameObject gameObject { get; }

    // AudioSource
    public AudioClip clip { get; set; }
    public float volume { get; set; }
    public float pitch { get; set; }
}