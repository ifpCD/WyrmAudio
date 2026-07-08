using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerGroupExtensions
{
    public static void Play(this AudioMixerGroup mixerGroup, AbstractWyrmBank bank, Transform track = default, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, bank, track, volume);
    }

    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, Transform track = default, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, clip, track, volume);
    }

    public static void PlayOneShot(this AudioMixerGroup mixerGroup, AudioClip clip, Transform track = default, float volume = 1f)
    {
        WyrmPoolController.PlayOneShot(mixerGroup, clip, track, volume);
    }

    public static void Play(this AudioMixerGroup mixerGroup, AbstractWyrmBank bank, Vector3 place, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, bank, place, volume);
    }

    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, Vector3 place, float volume = 1f)
    {
        WyrmPoolController.Play(mixerGroup, clip, place, volume);
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