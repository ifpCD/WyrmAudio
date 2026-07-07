using UnityEngine;

public interface IWyrmBank
{
    bool Loop { get; }
    
    bool FadeIn { get; }
    bool FadeOut { get; }
    
    bool PitchRandomization { get; }
    
    float PitchDeviation { get; }
    
    bool HasStart { get; }
    bool HasBody { get; }
    bool HasEnd { get; }

    AudioClip GetStartClip();
    AudioClip GetBodyClip();
    AudioClip GetEndClip();
}