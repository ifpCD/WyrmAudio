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

    public bool HasStart => startingClips != null && startingClips.Length > 0;
    public bool HasBody  => mainBodyClips != null && mainBodyClips.Length > 0;
    public bool HasEnd   => endingClips != null && endingClips.Length > 0;

    private readonly ShuffleBag<AudioClip> _startBag = new();
    private readonly ShuffleBag<AudioClip> _bodyBag  = new();
    private readonly ShuffleBag<AudioClip> _endBag   = new();

    private void OnEnable()
    {
        _startBag.SetSource(startingClips);
        _bodyBag.SetSource(mainBodyClips);
        _endBag.SetSource(endingClips);
    }

    private void OnValidate()
    {
        pitchDeviation = Mathf.Clamp(pitchDeviation, minPitchDeviation, maxPitchDeviation);

        _startBag.SetSource(startingClips);
        _bodyBag.SetSource(mainBodyClips);
        _endBag.SetSource(endingClips);
    }

    public AudioClip GetStartClip() => _startBag.Next();
    public AudioClip GetBodyClip()  => _bodyBag.Next();
    public AudioClip GetEndClip()   => _endBag.Next();
}