using UnityEngine;
using UnityEngine.Audio;

// csharpier-ignore
public partial class WyrmPoolController : MonoBehaviour
{
    public static void Play         (AudioMixerGroup mixerGroup, AbstractWyrmBank bank, Transform track = null, float? volume = null)   => GetPool(mixerGroup).Play(bank, track, volume);
    public static void Play         (AudioMixerGroup mixerGroup, AudioClip clip, Transform track = null, float? volume = null)          => GetPool(mixerGroup).Play(clip, track, volume);
    public static void Play         (AudioMixerGroup mixerGroup, AbstractWyrmBank bank, Vector3 position, float? volume = null)         => GetPool(mixerGroup).Play(bank, position, volume);
    public static void Play         (AudioMixerGroup mixerGroup, AudioClip clip, Vector3 position, float? volume = null)                => GetPool(mixerGroup).Play(clip, position, volume);
    public static bool TryBorrow    (AudioMixerGroup mixerGroup, out IWyrmSource pooledSource)                                          => GetPool(mixerGroup).TryBorrow(out pooledSource);
    public static void Return       (AudioMixerGroup mixerGroup, IWyrmSource pooledSource)                                              => GetPool(mixerGroup).ReturnToAvailable(pooledSource);
}
