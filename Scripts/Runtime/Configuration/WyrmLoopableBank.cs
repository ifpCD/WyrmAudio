using UnityEngine;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Loopable Bank")]
public class WyrmLoopableBank : ScriptableObject, IWyrmBank
{
    [Header("Banks")]
    public AudioClip[] startingClips;
    public AudioClip[] mainBodyClips;
    public AudioClip[] endingClips;

    [Header("Playback")]
    [field: SerializeField] public bool Loop { get; }

    [Header("Fading")]
    [field: SerializeField] public bool FadeIn { get; }
    [field: SerializeField] public bool FadeOut { get; }

    [Header("Pitch")]
    [field: SerializeField] public bool PitchRandomization { get; }

    [Range(MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE)]
    [field: SerializeField] public float PitchDeviation { get; private set; }

    const float MIN_PITCH_DEVIATION_RANGE = 0.01f;
    const float MAX_PITCH_DEVIATION_RANGE = 0.5f;

    public bool HasStart => startingClips != null && startingClips.Length > 0;
    public bool HasBody => mainBodyClips != null && mainBodyClips.Length > 0;
    public bool HasEnd => endingClips != null && endingClips.Length > 0;

    private readonly ShuffleBag<AudioClip> _startBag = new();
    private readonly ShuffleBag<AudioClip> _bodyBag = new();
    private readonly ShuffleBag<AudioClip> _endBag = new();

    private void OnEnable()
    {
        _startBag.SetSource(startingClips);
        _bodyBag.SetSource(mainBodyClips);
        _endBag.SetSource(endingClips);
    }

    private void OnValidate()
    {
        PitchDeviation = Mathf.Clamp(PitchDeviation, MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE);

        _startBag.SetSource(startingClips);
        _bodyBag.SetSource(mainBodyClips);
        _endBag.SetSource(endingClips);
    }

    public AudioClip GetStartClip() => _startBag.Next();
    public AudioClip GetBodyClip() => _bodyBag.Next();
    public AudioClip GetEndClip() => _endBag.Next();
}