using UnityEngine;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Sound Bank")]
public class WyrmSoundBank : ScriptableObject
{
    [Header("Banks")]
    public AudioClip[] startingClips;
    public AudioClip[] mainBodyClips;
    public AudioClip[] endingClips;

    [Header("Playback")]
    public bool loop;

    [Header("Fading")]
    public bool fadeIn;
    public bool fadeOut;

    [Header("Pitch")]
    public bool pitchRandomization;

    [Range(minPitchDeviation, maxPitchDeviation)]
    public float pitchDeviation;

    const float minPitchDeviation = 0.01f;
    const float maxPitchDeviation = 0.07f;

    public bool HasStart { get; private set; }
    public bool HasBody { get; private set; }
    public bool HasEnd { get; private set; }

    private void OnValidate()
    {
        HasStart = startingClips != null && startingClips.Length > 0;
        HasBody = mainBodyClips != null && mainBodyClips.Length > 0;
        HasEnd = endingClips != null && endingClips.Length > 0;

        pitchDeviation = Mathf.Clamp(pitchDeviation, minPitchDeviation, maxPitchDeviation);
    }
}