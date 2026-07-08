using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Simple Bank")]
public class WyrmSimpleBank : AbstractWyrmBank
{
    [Header("Banks")]
    public AudioClip[] mainBodyClips;

    [Header("Playback")]
    [field: SerializeField]
    public override bool Loop { get; protected set; }

    [Header("Fading")]
    [field: SerializeField] public override bool FadeIn { get; protected set; }
    [field: SerializeField] public override bool FadeOut { get; protected set; }

    [Header("Pitch")]
    [field: SerializeField] public override bool PitchRandomization { get; protected set; }

    [Range(MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE)]
    [field: SerializeField] public override float PitchDeviation { get; protected set; }

    const float MIN_PITCH_DEVIATION_RANGE = 0.01f;
    const float MAX_PITCH_DEVIATION_RANGE = 0.5f;

    public override bool HasStart => false;
    public override bool HasBody => mainBodyClips != null && mainBodyClips.Length > 0;
    public override bool HasEnd => false;

    private readonly ShuffleBag<AudioClip> _bodyBag = new();

    private void OnEnable()
    {
        _bodyBag.SetSource(mainBodyClips);
    }

    private void OnValidate()
    {
        PitchDeviation = Mathf.Clamp(PitchDeviation, MIN_PITCH_DEVIATION_RANGE, MAX_PITCH_DEVIATION_RANGE);

        _bodyBag.SetSource(mainBodyClips);
    }

    public override AudioClip GetStartClip() => null;
    public override AudioClip GetBodyClip() => _bodyBag.Next();
    public override AudioClip GetEndClip() => null;
}