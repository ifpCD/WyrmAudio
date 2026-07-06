using SteamAudio;
using UnityEngine;

public class WyrmBaseSteamSource : WyrmBaseSource
{
    public SteamAudioSource SteamSrc { get; private set; }

    protected override void Awake()
    {
        SteamSrc = gameObject.GetComponent<SteamAudioSource>();
        base.Awake();
    }

    public override void Play()
    {
        // SteamSrc.enabled = true;
        base.Play();
    }

    public override void PlayOneShot(AudioClip clip)
    {
        // SteamSrc.enabled = true;
        base.PlayOneShot(clip);
    }

    public override void Deactivate()
    {
        SteamSrc.enabled = false;
        base.Deactivate();
    }
}