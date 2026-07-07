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

    [Range(MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE)]
    public float pitchDeviation;

    const float MIN_PITCH_DEVIATION_RANGE = 0.01f;
    const float MAX_PITCH_DEVIATION_RANGE = 0.5f;

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
        pitchDeviation = Mathf.Clamp(pitchDeviation, MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE);

        _startBag.SetSource(startingClips);
        _bodyBag.SetSource(mainBodyClips);
        _endBag.SetSource(endingClips);
    }

    public AudioClip GetStartClip() => _startBag.Next();
    public AudioClip GetBodyClip()  => _bodyBag.Next();
    public AudioClip GetEndClip()   => _endBag.Next();
}