using UnityEngine;

public abstract class AbstractWyrmBank : ScriptableObject
{
    public abstract bool Loop { get; protected set; }
    
    public abstract bool FadeIn { get;protected set;  }
    public abstract bool FadeOut { get; protected set; }
    
    public abstract bool PitchRandomization { get; protected set; }
    
    public abstract float PitchDeviation { get; protected set; }
    
    public abstract bool HasStart { get; }
    public abstract bool HasBody { get; }
    public abstract bool HasEnd { get; }

    public abstract AudioClip GetStartClip();
    public abstract AudioClip GetBodyClip();
    public abstract AudioClip GetEndClip();
}