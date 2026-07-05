using UnityEngine;

public class BasePooledSource : IPooledAudioSource
{
    public AudioSource ASource;

    void Update()
    {
        if (TrackedTransform != null)
            gameObject.transform.SetPositionAndRotation(TrackedTransform.position, TrackedTransform.rotation);
    }

    public override float Pitch => ASource.pitch;
    public override bool IsPlaying => ASource.isPlaying;
    public override Transform TrackedTransform { get; set; }

    public override int PlayVersion { get; set; } = 0;

    public override void Play() => ASource.Play();
    public override void PlayOneShot(AudioClip clip) => ASource.PlayOneShot(clip);
    public override void Stop() => ASource.Stop();

    public override void SetClip(AudioClip clip) => ASource.clip = clip;
    public override void SetVolume(float volume) => ASource.volume = volume;
    public override void SetPitch(float pitch) => ASource.pitch = pitch;
}
