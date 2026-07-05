using SteamAudio;
using UnityEngine;

public class PooledSteamAudioSource : BasePooledSource
{
    SteamAudioSource steamSrc;

    protected override void Awake()
    {
        steamSrc = gameObject.GetComponent<SteamAudioSource>();
        base.Awake();
    }

    public override void Play()
    {
        steamSrc.enabled = true;
        base.Play();
    }

    public override void PlayOneShot(AudioClip clip)
    {
        steamSrc.enabled = true;
        base.PlayOneShot(clip);
    }

    public override void Deactivate()
    {
        steamSrc.enabled = false;
        base.Deactivate();
    }
}