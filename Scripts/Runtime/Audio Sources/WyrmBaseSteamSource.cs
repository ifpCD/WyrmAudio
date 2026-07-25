using System;
using SteamAudio;
using UnityEngine;

[RequireComponent(typeof(SteamAudioSource))]
public sealed class WyrmBaseSteamSource : WyrmBaseSource
{
    [HideInInspector]
    public SteamAudioSource SteamSource { get; set; }

    protected override void OnValidate()
    {
        base.OnValidate();
        SteamSource = SteamSource != null ? SteamSource : GetComponent<SteamAudioSource>();
    }

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

    internal override bool Deactivate()
    {
        if (!base.Deactivate())
            return false;

        SteamSource.enabled = false;
        return true;
    }
}
