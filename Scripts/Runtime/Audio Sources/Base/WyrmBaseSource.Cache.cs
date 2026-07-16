using UnityEngine;
#pragma warning disable IDE1006 // MonoBehaviour and AudioSource

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    private AudioClip _clip;
    private bool _loop;
    private float _volume;
    private float _pitch;
    private float _minDistance;
    private float _maxDistance;

    public AudioClip clip
    {
        get => _clip;
        set
        {
            if (_clip == value) return;

            _clip = value;
            ASource.clip = value;
        }
    }


    public bool loop
    {
        get => _loop;
        set
        {
            if (_loop == value) return;

            _loop = value;
            ASource.loop = value;
        }
    }

    public float volume
    {
        get => _volume;
        set
        {
            if (_volume == value) return;

            _volume = value;
            ASource.volume = value;
        }
    }

    public float pitch
    {
        get => _pitch;
        set
        {
            if (_pitch == value) return;

            _pitch = value;
            ASource.pitch = value;
        }
    }

    public float minDistance
    {
        get => _minDistance;
        set
        {
            if (_minDistance == value) return;

            _minDistance = value;
            ASource.minDistance = value;

            if (ActiveIndex == -1) return;
            WyrmPoolController.Instance.MinDistances[ActiveIndex] = value;
        }
    }

    public float maxDistance
    {
        get => _maxDistance;
        set
        {
            if (_maxDistance == value) return;

            _maxDistance = value;
            ASource.maxDistance = value;

            if (ActiveIndex == -1) return;
            WyrmPoolController.Instance.MaxDistances[ActiveIndex] = value;
        }
    }
}