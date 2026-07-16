// csharpier-ignore
public partial class WyrmPhononSource : WyrmBaseSource
{
    const int APPLY_DISTANCEATTENUATION    = 0;
    const int APPLY_AIRABSORPTION          = 1;
    const int APPLY_DIRECTIVITY            = 2;
    const int APPLY_OCCLUSION              = 3;
    const int APPLY_TRANSMISSION           = 4;
    const int APPLY_REFLECTIONS            = 5;
    const int APPLY_PATHING                = 6;
    const int HRTF_INTERPOLATION           = 7;
    const int DISTANCEATTENUATION          = 8;
    const int DISTANCEATTENUATION_USECURVE = 9;
    const int AIRABSORPTION_LOW            = 10;
    const int AIRABSORPTION_MID            = 11;
    const int AIRABSORPTION_HIGH           = 12;
    const int AIRABSORPTION_USERDEFINED    = 13;
    const int DIRECTIVITY                  = 14;
    const int DIRECTIVITY_DIPOLEWEIGHT     = 15;
    const int DIRECTIVITY_DIPOLEPOWER      = 16;
    const int DIRECTIVITY_USERDEFINED      = 17;
    const int OCCLUSION                    = 18;
    const int TRANSMISSION_TYPE            = 19;
    const int TRANSMISSION_LOW             = 20;
    const int TRANSMISSION_MID             = 21;
    const int TRANSMISSION_HIGH            = 22;
    const int DIRECT_MIXLEVEL              = 23;
    const int REFLECTIONS_BINAURAL         = 24;
    const int REFLECTIONS_MIXLEVEL         = 25;
    const int PATHING_BINAURAL             = 26;
    const int PATHING_MIXLEVEL             = 27;
    const int SIMULATION_OUTPUTS_PTR_LOW   = 28;
    const int SIMULATION_OUTPUTS_PTR_HIGH  = 29;
    const int DIRECT_BINAURAL              = 30;
    const int SIMULATION_OUTPUTS_HANDLE    = 31;
    const int PERSPECTIVE_CORRECTION       = 32;
    const int PATHING_NORMALIZEEQ          = 33;
    const int NUM_PARAMS                   = 34;

    private const float TRUE               = 1f;
    private const float FALSE              = 0f;
}
