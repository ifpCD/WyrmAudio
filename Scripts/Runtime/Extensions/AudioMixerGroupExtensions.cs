using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerGroupExtensions
{
    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, float volume = 1f, Transform track = default)
    {
        WyrmPoolController.Play(mixerGroup, clip, volume, track);
    }

    public static void Play(this AudioMixerGroup mixerGroup, WyrmSoundBank bank, float volume = 1f, Transform track = default)
    {
        WyrmPoolController.Play(mixerGroup, bank.mainBodyClips[0], volume, track);
    }

    public static void PlayOneShot(this AudioMixerGroup mixerGroup, AudioClip clip, float volume = 1f, Transform track = default)
    {
        WyrmPoolController.PlayOneShot(mixerGroup, clip, volume, track);
    }

    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, Vector3 place, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, clip, place, volume);
    }

    public static void Play(this AudioMixerGroup mixerGroup, WyrmSoundBank bank, Vector3 place, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, bank.mainBodyClips[0], place, volume);
    }

    public static void PlayOneShot(this AudioMixerGroup mixerGroup, AudioClip clip, Vector3 place, float volume = 1f)
    {
        WyrmPoolController.PlayOneShot(mixerGroup, clip, place, volume);
    }


    public static bool TryBorrow(this AudioMixerGroup mixerGroup, out IWyrmSource pooledAudioSource)
    {
        return WyrmPoolController.TryBorrow(mixerGroup, out pooledAudioSource);
    }

    public static void Return(this AudioMixerGroup mixerGroup, IWyrmSource pooledAudioSource)
    {
        WyrmPoolController.Return(mixerGroup, pooledAudioSource);
    }
}