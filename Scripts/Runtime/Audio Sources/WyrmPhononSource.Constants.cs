using System;
using UnityEngine;
using SteamAudio;
using Unity.Mathematics;

public partial class WyrmPhononSource : WyrmBaseSource
{
    private const int APPLY_DISTANCEATTENUATION             = 0;
    private const int APPLY_AIRABSORPTION                   = 1;
    private const int APPLY_DIRECTIVITY                      = 2;
    private const int APPLY_OCCLUSION                        = 3;
    private const int APPLY_TRANSMISSION                     = 4;
    private const int APPLY_REFLECTIONS                      = 5;
    private const int APPLY_PATHING                          = 6;
    private const int HRTF_INTERPOLATION               = 7;

    private const int USER_DEFINED_DIRECTIVITY         = 17;
    private const int OCCLUSION_VALUE                  = 18;
    private const int FREQUENCY_DEPENDENT_TRANSMISSION = 19;
    private const int TRANSMISSION_LOW                 = 20;
    private const int TRANSMISSION_MID                 = 21;
    private const int TRANSMISSION_HIGH                = 22;

    private const int PATHING_BINAURAL                 = 26;

    private const int DIRECT_BINAURAL                  = 30;
    private const int PLUGIN_SOURCE_HANDLE             = 31;
    private const int PERSPECTIVE_CORRECTION           = 32;
    private const int NORMALIZE_PATHING_EQ             = 33;

    private const float TRUE                           = 1f;
    private const float FALSE                          = 0f;
}