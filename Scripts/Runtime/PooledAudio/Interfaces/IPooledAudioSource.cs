using UnityEngine;
#pragma warning disable IDE1006 // gameObject MonoBehaviour

public interface IPooledAudioSource
{
    public float Pitch { get; }
    public bool IsPlaying { get; }
    public Transform TrackedTransform { get; set; }
    public bool IsOneShot { get; set; }
    public int PlayVersion { get; set; }
    public void Play();
    public void PlayOneShot(AudioClip clip);
    public void SetClip(AudioClip clip);
    public void SetVolume(float volume);
    public void SetPitch(float pitch);
    public void Stop();

    public GameObject gameObject { get; }
}