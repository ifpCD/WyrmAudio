using SaintsField.Playa;
using UnityEngine;

public partial class WyrmBaseSource : AmbiMonoBehaviour<WyrmBaseSource>, IWyrmSource
{
    [Header("Spherical Harmonics")]
    [SerializeField]
    Vector3 _virtualPosition = new();

    [SerializeField]
    [Range(0, 1)]
    float _verticalWidth = 0f;

    [SerializeField]
    [Range(0, 1)]
    float _horizontalWidth = 0f;

    [SerializeField]
    [Range(0, 5)]
    float _directionalGain = 1f;

    [SerializeField]
    [Range(0, 5)]
    float _ambientGain = 1f;

    [SerializeField]
    [Range(0, 1)]
    float _ambisonicEQLow01 = 1f;

    [SerializeField]
    [Range(0, 1)]
    float _ambisonicEQMid01 = 1f;

    [SerializeField]
    [Range(0, 1)]
    float _ambisonicEQHigh01 = 1f;

    public Vector3 VirtualPosition
    {
        get => _virtualPosition;
        set
        {
            if (_virtualPosition == value)
                return;

            _virtualPosition = value;

            if (IsRegistered)
                InputVirtualPositions[SoAIndex] = value;
        }
    }

    public float VerticalWidth
    {
        get => _verticalWidth;
        set
        {
            if (_verticalWidth == value)
                return;

            _verticalWidth = value;

            if (IsRegistered)
                InputVerticalWidths[SoAIndex] = value;
        }
    }

    public float HorizontalWidth
    {
        get => _horizontalWidth;
        set
        {
            if (_horizontalWidth == value)
                return;

            _horizontalWidth = value;

            if (IsRegistered)
                InputHorizontalWidths[SoAIndex] = value;
        }
    }

    public float DirectionalGain
    {
        get => _directionalGain;
        set
        {
            if (_directionalGain == value)
                return;

            _directionalGain = value;

            if (IsRegistered)
                InputDirectionalGains[SoAIndex] = value;
        }
    }

    public float AmbientGain
    {
        get => _ambientGain;
        set
        {
            if (_ambientGain == value)
                return;

            _ambientGain = value;

            if (IsRegistered)
                InputAmbientGains[SoAIndex] = value;
        }
    }

    public float AmbisonicEQLow01
    {
        get => _ambisonicEQLow01;
        set
        {
            if (_ambisonicEQLow01 == value)
                return;

            _ambisonicEQLow01 = value;

            if (IsRegistered)
                InputAmbisonicEQLow01s[SoAIndex] = value;
        }
    }

    public float AmbisonicEQMid01
    {
        get => _ambisonicEQMid01;
        set
        {
            if (_ambisonicEQMid01 == value)
                return;

            _ambisonicEQMid01 = value;

            if (IsRegistered)
                InputAmbisonicEQMid01s[SoAIndex] = value;
        }
    }

    public float AmbisonicEQHigh01
    {
        get => _ambisonicEQHigh01;
        set
        {
            if (_ambisonicEQHigh01 == value)
                return;

            _ambisonicEQHigh01 = value;

            if (IsRegistered)
                InputAmbisonicEQHigh01s[SoAIndex] = value;
        }
    }
}
