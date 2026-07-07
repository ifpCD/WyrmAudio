using UnityEngine;

[CreateAssetMenu(menuName = "Wyrm Audio/Wyrm Simple Bank")]
public class WyrmSimpleBank : ScriptableObject, IWyrmBank
{
    [Header("Banks")]
    public AudioClip[] mainBodyClips;

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

    public bool HasStart => false;
    public bool HasBody => mainBodyClips != null && mainBodyClips.Length > 0;
    public bool HasEnd => false;

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

    public AudioClip GetStartClip() => null;
    public AudioClip GetBodyClip() => _bodyBag.Next();
    public AudioClip GetEndClip() => null;
}