using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public interface IWyrmSource
{
    bool UseReflections { get; set; }
    bool UseAmbisonics { get; set; }
    bool UseOcclusion { get; set; }

    Transform TrackedTransform { get; set; }

    void Play(AudioClip clip, Transform track = null, float? volume = null);
    void Play(AbstractWyrmBank clip, Transform track = null, float? volume = null);

    void Return();

    bool IsBorrowed { get; }

    Transform CachedTransform { get; }

    float TargetVolume { get; set; }

    // MonoBehaviour
    GameObject gameObject { get; }

    // AudioSource
    // AudioSource ASource { get; set; }
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
