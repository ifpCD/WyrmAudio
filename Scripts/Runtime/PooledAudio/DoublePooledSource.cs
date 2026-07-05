using UnityEngine;

public class DoublePooledSource : BasePooledSource
{
    public AudioSource BSource;

    public override void Play()
    {
        BSource.Play();
        base.Play();
    }

    public override void Deactivate()
    {
        BSource.Stop();
        base.Deactivate();
    }

    public override AudioClip clip
    {
        set
        {
            base.clip = value;
            BSource.clip = value;
        }
    }

    public override float volume
    {
        set
        {
            base.volume = value;
            BSource.volume = value;
        }
    }

    public override float pitch
    {
        set
        {
            base.pitch = value;
            BSource.pitch = value;
        }
    }
}