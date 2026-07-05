using UnityEngine;

public class BasePooledSource : MonoBehaviour, IPooledAudioSource
{
    void Start()
    {
    }

    void Update()
    {

    }

    public void Play() { }
    public void SetClip(AudioClip clip) { }
    public void SetVolume(float volume) { }
}
