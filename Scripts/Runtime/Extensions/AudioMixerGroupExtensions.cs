using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerGroupExtensions
{
    public static void Play(this AudioMixerGroup mixerGroup, AudioClip clip, float volume = 1f, Transform transform = default)
    {
        WyrmPool.Play(mixerGroup, clip, volume, transform);
    }

    public static bool TryBorrow(this AudioMixerGroup mixerGroup, out IPooledAudioSource pooledAudioSource)
    {
        return WyrmPool.TryBorrow(mixerGroup, out pooledAudioSource);
    }

    public static void Return(this AudioMixerGroup mixerGroup, IPooledAudioSource pooledAudioSource)
    {
        WyrmPool.Return(mixerGroup, pooledAudioSource);
    }
}