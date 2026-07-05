using UnityEngine;

public class BasePooledSource : IPooledAudioSource
{
    void Start()
    {
    }

    void Update()
    {

    }

    public override int PlayVersion { get; set; } = 0;
    
    public override void Play() { }
    public override void SetClip(AudioClip clip) { }
    public override void SetVolume(float volume) { }
}
