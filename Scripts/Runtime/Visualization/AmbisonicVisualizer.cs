using UnityEngine;
using UnityEngine.Audio;

public enum AmbisonicVisualization : byte
{
    Encoded,
    Rendered,
}

// Sum of every audible source's banded field around the listener. The field sphere is world-aligned; the virtual speakers
// are head-locked like Phonon's and fed by its sampling decoder. Rendered applies Phonon's per-band max-rE weighting,
// Encoded shows the raw coefficients. Negative lobes and negative speaker feeds are black.
[DefaultExecutionOrder(200)]
public sealed partial class AmbisonicVisualizer : MonoBehaviour
{
    [Header("Field")]
    public AudioMixerGroup targetMixerGroup;

    public AmbisonicVisualization visualization = AmbisonicVisualization.Rendered;

    public bool weightByVolume = true;

    public Vector3 bandVisibility = Vector3.one;

    public float sensitivity = 25.0f;

    public float baseRadius = 2.0f;

    public float deformationScale = 1.5f;

    [Range(0f, 0.2f)]
    public float idleOpacity = 0.02f;

    [Range(0f, 1f)]
    public float gridOpacity = 0.25f;

    [Range(3, 6)]
    public int subdivisions = 5;

    [Header("Virtual Speakers")]
    public bool showVirtualSpeakers = true;

    public float speakerRadius = 4.0f;

    public float speakerSize = 0.12f;

    [Range(12f, 72f)]
    public float speakerRangeDecibels = 36f;

    [Range(0f, 1f)]
    public float speakerIdleOpacity = 0.15f;

    [Header("Diagnostics")]
    public bool showEnergyVectors = true;

    [Range(0, 16)]
    public int labeledSpeakers = 4;

    [Header("Colors")]
    [ColorUsage(true, true)]
    public Color idleColor = new(0.02f, 0.1f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color lowFreqColor = new(1.0f, 0.05f, 0.0f, 1.0f);

    [ColorUsage(true, true)]
    public Color midFreqColor = new(0.1f, 1.0f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color highFreqColor = new(0.0f, 0.8f, 1.0f, 1.0f);

#if UNITY_EDITOR
    Mesh _sphere;
    int _sphereSubdivisions;
    Material _material;
    MaterialPropertyBlock _properties;

    bool _hasFrame;
    Vector3 _listenerPosition;
    Quaternion _listenerRotation;

    static readonly int FIELD_ID = Shader.PropertyToID("_Field");
    static readonly int ORDER_ID = Shader.PropertyToID("_Order");
    static readonly int BASE_RADIUS_ID = Shader.PropertyToID("_BaseRadius");
    static readonly int DEFORM_SCALE_ID = Shader.PropertyToID("_DeformScale");
    static readonly int SENSITIVITY_ID = Shader.PropertyToID("_Sensitivity");
    static readonly int IDLE_OPACITY_ID = Shader.PropertyToID("_IdleOpacity");
    static readonly int GRID_INTENSITY_ID = Shader.PropertyToID("_GridIntensity");

    static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
    static readonly int LOW_COLOR_ID = Shader.PropertyToID("_LowColor");
    static readonly int MID_COLOR_ID = Shader.PropertyToID("_MidColor");
    static readonly int HIGH_COLOR_ID = Shader.PropertyToID("_HighColor");

    void OnEnable()
    {
        AllocateField();
        AllocateSpeakers();
    }

    void OnDisable()
    {
        _hasFrame = false;

        DeallocateField();
        DeallocateSpeakers();

        if (_material != null)
            DestroyImmediate(_material);

        if (_sphere != null)
            DestroyImmediate(_sphere);

        if (_speakerMaterial != null)
            DestroyImmediate(_speakerMaterial);

        if (_speakerMesh != null)
            DestroyImmediate(_speakerMesh);
    }

    void LateUpdate()
    {
        _hasFrame = false;

        if (!Application.isPlaying || WyrmBaseSource.CompletelyInactive || WyrmAudioManager.Listener == null)
            return;

        if (!EnsureFieldResources() || !EnsureSpeakerResources())
            return;

        _listenerPosition = WyrmListener.ListenerPosition.Value;
        _listenerRotation = WyrmListener.ListenerRotation.Value;

        int order = AccumulateField();
        DecodeSpeakers(order, _listenerRotation);

        DrawField(order);

        if (showVirtualSpeakers)
            DrawSpeakers();

        _hasFrame = true;
    }

    bool EnsureFieldResources()
    {
        _properties ??= new MaterialPropertyBlock();

        if (_material == null)
        {
            Shader shader = Shader.Find("Hidden/WyrmAudio/AmbisonicVisualizer");

            if (shader == null)
                return false;

            _material = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (_sphere == null || _sphereSubdivisions != subdivisions)
        {
            if (_sphere != null)
                DestroyImmediate(_sphere);

            _sphere = BuildIcosphere(subdivisions);
            _sphereSubdivisions = subdivisions;
        }

        return true;
    }

    void DrawField(int order)
    {
        _properties.SetVectorArray(FIELD_ID, _fieldUpload);
        _properties.SetFloat(ORDER_ID, order);
        _properties.SetFloat(BASE_RADIUS_ID, baseRadius);
        _properties.SetFloat(DEFORM_SCALE_ID, deformationScale);
        _properties.SetFloat(SENSITIVITY_ID, sensitivity);
        _properties.SetFloat(IDLE_OPACITY_ID, idleOpacity);
        _properties.SetFloat(GRID_INTENSITY_ID, gridOpacity);

        _properties.SetColor(BASE_COLOR_ID, idleColor);
        _properties.SetColor(LOW_COLOR_ID, lowFreqColor);
        _properties.SetColor(MID_COLOR_ID, midFreqColor);
        _properties.SetColor(HIGH_COLOR_ID, highFreqColor);

        Graphics.DrawMesh(_sphere, Matrix4x4.Translate(_listenerPosition), _material, gameObject.layer, null, 0, _properties);
    }
#endif
}
