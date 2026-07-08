using UnityEngine;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Loopable Bank")]
public class WyrmLoopableBank : AbstractWyrmBank
{
    [Header("Banks")]
    public AudioClip[] startingClips;
    public AudioClip[] mainBodyClips;
    public AudioClip[] endingClips;

    [Header("Playback")]
    [field: SerializeField] public override bool Loop { get; protected set; }

    [Header("Fading")]
    [field: SerializeField] public override bool FadeIn { get; protected set; }
    [field: SerializeField] public override bool FadeOut { get; protected set; }

    [Header("Pitch")]
    [field: SerializeField] public override bool PitchRandomization { get; protected set; }

    [Range(MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE)]
    [field: SerializeField]
    public override float PitchDeviation { get; protected set; }

    const float MIN_PITCH_DEVIATION_RANGE = 0.01f;
    const float MAX_PITCH_DEVIATION_RANGE = 0.5f;

    public override bool HasStart => startingClips != null && startingClips.Length > 0;
    public override bool HasBody => mainBodyClips != null && mainBodyClips.Length > 0;
    public override bool HasEnd => endingClips != null && endingClips.Length > 0;

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

    public override AudioClip GetStartClip() => _startBag.Next();
    public override AudioClip GetBodyClip() => _bodyBag.Next();
    public override AudioClip GetEndClip() => _endBag.Next();
}