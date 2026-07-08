using System;
using SteamAudio;
using UnityEngine;

public class WyrmBaseSteamSource : WyrmBaseSource
{
    [field: SerializeField]
    public SteamAudioSource SteamSource { get; set; }

    protected override void Awake()
    {
        SteamSource.enabled = true;
        base.Awake();
    }

    public override void Play()
    {
        SteamSource.enabled = true;
        base.Play();
    }

    public override void PlayOneShot(AudioClip clip)
    {
        SteamSource.enabled = true;
        base.PlayOneShot(clip);
    }

    public override void Deactivate()
    {
        base.Deactivate();
        SteamSource.enabled = false;
    }
}