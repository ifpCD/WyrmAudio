using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Audio Clip")]
public class WyrmAudioClip : ScriptableObject
{
    [Header("Banks")]
    public AudioClip[] mainBodyClips;
    public AudioClip[] startingClips;
    public AudioClip[] endingClips;

    [Header("Playback")]
    public bool loop;

    [Header("Fading")]
    public bool fadeIn;
    public bool fadeOut;

    [Header("Pitch")]
    public bool pitchRandomization;

    [Range(0.01f, 0.07f)]
    public float pitchDeviation;
}
