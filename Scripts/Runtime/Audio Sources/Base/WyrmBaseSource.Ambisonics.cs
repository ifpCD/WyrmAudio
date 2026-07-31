using SaintsField.Playa;
using UnityEngine;

public partial class WyrmBaseSource : AmbiMonoBehaviour<WyrmBaseSource>, IWyrmSource
{
    Vector3 _virtualPosition = new();
    float _verticalWidth = 0f;
    float _horizontalWidth = 0f;
    float _directionalGain = 1f;
    float _ambientGain = 1f;
    float _ambisonicEQLow01 = 1f;
    float _ambisonicEQMid01 = 1f;
    float _ambisonicEQHigh01 = 1f;

    [ShowInInspector]
    public Vector3 VirtualPosition
    {
        get => _virtualPosition;
        set
        {
            if (_virtualPosition == value)
                return;

            _virtualPosition = value;

            if (IsRegistered)
                InputVirtualPositions[NativeIndex] = value;
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
                InputVerticalWidths[NativeIndex] = value;
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
                InputHorizontalWidths[NativeIndex] = value;
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
                InputDirectionalGains[NativeIndex] = value;
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
                InputAmbientGains[NativeIndex] = value;
        }
    }

    [ShowInInspector]
    [Range(0, 1f)]
    public float AmbisonicEQLow01
    {
        get => _ambisonicEQLow01;
        set
        {
            if (_ambisonicEQLow01 == value)
                return;

            _ambisonicEQLow01 = value;

            if (IsRegistered)
                InputAmbisonicEQLow01s[NativeIndex] = value;
        }
    }

    [ShowInInspector]
    [Range(0, 1f)]
    public float AmbisonicEQMid01
    {
        get => _ambisonicEQMid01;
        set
        {
            if (_ambisonicEQMid01 == value)
                return;

            _ambisonicEQMid01 = value;

            if (IsRegistered)
                InputAmbisonicEQMid01s[NativeIndex] = value;
        }
    }

    [ShowInInspector]
    [Range(0, 1f)]
    public float AmbisonicEQHigh01
    {
        get => _ambisonicEQHigh01;
        set
        {
            if (_ambisonicEQHigh01 == value)
                return;

            _ambisonicEQHigh01 = value;

            if (IsRegistered)
                InputAmbisonicEQHigh01s[NativeIndex] = value;
        }
    }
}
