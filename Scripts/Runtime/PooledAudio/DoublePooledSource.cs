using UnityEngine;

public class DoublePooledSource : BasePooledSource
{
    public AudioSource BSource;

    public override void Play()
    {
        BSource.Play();
        base.Play();
    }
    public override void Stop()
    {
        BSource.Stop();
        base.Stop();
    }

    public override void SetClip(AudioClip clip)
    {
        BSource.clip = clip;
        base.SetClip(clip);
    }
    public override void SetVolume(float volume)
    {
        BSource.volume = volume;
        base.SetVolume(volume);
    }
    public override void SetPitch(float pitch)
    {
        BSource.pitch = pitch;
        base.SetPitch(pitch);
    }
}
