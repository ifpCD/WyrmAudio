internal static class HC // Hardcode
{
    public const int MAX_AMBISONIC_CONTRIBUTORS = 2000;

    public const int MAX_OCC_MASKS = 8000;
    public const int MAX_OCC_SAMPLES_PER_MASK = 64;
    public const int MAX_OCC_SAMPLES = MAX_OCC_MASKS * MAX_OCC_SAMPLES_PER_MASK;

    // must match phonon's IPL_AMBISONIC_FIELD_MAX_ORDER and IPL_NUM_BANDS
    public const int MAX_AMBISONIC_ORDER = 10;
    public const int MAX_AMBISONIC_BANDS = 3;

    public const int MAX_AMBISONIC_CHANNELS = (MAX_AMBISONIC_ORDER + 1) * (MAX_AMBISONIC_ORDER + 1);
    public const int AMBISONIC_BUFFER_LENGTH = MAX_AMBISONIC_BANDS * MAX_AMBISONIC_CHANNELS;

    public const float PLAYBACK_END_GRACE_TIME = 0.1f;
}
