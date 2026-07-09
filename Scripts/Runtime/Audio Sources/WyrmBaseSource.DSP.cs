using System;
using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    private volatile float _targetVolume = 1f;
    private volatile float _currentVolume = 1f;
    private float _volumeAlpha;

    public float TargetVolume
    {
        get => _targetVolume;
        set => _targetVolume = Mathf.Clamp01(value);
    }

    [SerializeField] private float volumeTransitionTime = 0.05f;

    public float VolumeTransitionTime
    {
        get => volumeTransitionTime;
        set
        {
            volumeTransitionTime = Mathf.Max(0f, value);
            CalculateVolumeAlpha();
        }
    }

    private void CalculateVolumeAlpha()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        if (sampleRate > 0 && volumeTransitionTime > 0f)
        {
            _volumeAlpha = 1f - Mathf.Exp(-1f / (sampleRate * volumeTransitionTime));
        }
        else
        {
            _volumeAlpha = 1f;
        }
    }

    void OnAudioFilterRead(float[] data, int channels) => ApplyVolumeDSP(data, channels);

    protected void ApplyVolumeDSP(float[] data, int channels)
    {
        float target = _targetVolume;
        float current = _currentVolume;

        const float epsilonSquared = 1e-8f;

        // if we are close enough, snap
        float diff = target - current;
        if (diff * diff < epsilonSquared)
            current = target;

        if (current == target)
        {
            if (current == 1f) return;

            if (current == 0f)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            for (int frameOffset = 0; frameOffset < data.Length; frameOffset++)
            {
                data[frameOffset] *= current;
            }
        }
        else
        {
            for (int frameOffset = 0; frameOffset < data.Length; frameOffset += channels)
            {
                current += (target - current) * _volumeAlpha;

                for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    data[frameOffset + channelIndex] *= current;
                }
            }
        }

        _currentVolume = current;
    }
}