using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerGroupExtensions
{
    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, float volume = 1f, Transform track = default)
    {
        WyrmPool.Play(mixerGroup, clip, volume, track);
    }

    public static void Play(this AudioMixerGroup mixerGroup, WyrmSoundBank clip, float volume = 1f, Transform track = default)
    {
        WyrmPool.Play(mixerGroup, clip, volume, track);
    }

    public static void PlayOneShot(this AudioMixerGroup mixerGroup, AudioClip clip, float volume = 1f, Transform track = default)
    {
        WyrmPool.PlayOneShot(mixerGroup, clip, volume, track);
    }

    public static bool TryBorrow(this AudioMixerGroup mixerGroup, out IWyrmSource pooledAudioSource)
    {
        return WyrmPool.TryBorrow(mixerGroup, out pooledAudioSource);
    }

    public static void Return(this AudioMixerGroup mixerGroup, IWyrmSource pooledAudioSource)
    {
        WyrmPool.Return(mixerGroup, pooledAudioSource);
    }
}