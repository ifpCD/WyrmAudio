using UnityEngine;
using UnityEngine.Audio;

public enum AmbisonicVisualization : byte
{
    Encoded,
    Rendered,
}

public enum AmbisonicRadiusScale : byte
{
    Decibel,
    Linear,
}

// Sum of every audible source's banded field around the listener, drawn as a directivity balloon: radius by level, color by
// band, evaluated per pixel. nodal lines trace the zero crossings and decibel contours drift outward from the peak.
// The virtual speakers are head-locked like Phonon's andfed by its sampling decoder.
// Rendered applies Phonon's per-band max-rE weighting, Encoded shows the raw coefficients.
[DefaultExecutionOrder(200)]
public sealed partial class AmbisonicVisualizer : MonoBehaviour
{
    [Header("Field")]
    public AudioMixerGroup targetMixerGroup;

    public AmbisonicVisualization visualization = AmbisonicVisualization.Rendered;

    public bool weightByVolume = true;

    public Vector3 bandVisibility = Vector3.one;

    [Header("Shape")]
    public AmbisonicRadiusScale radiusScale = AmbisonicRadiusScale.Decibel;

    [Range(6f, 60f)]
    public float rangeDecibels = 30f;

    public float linearSensitivity = 25.0f;

    public float baseRadius = 2.0f;

    public float deformationScale = 1.5f;

    [Range(3, 7)]
    public int subdivisions = 6;

    [Header("Surface")]
    [Range(0f, 1f)]
    public float idleOpacity = 0.04f;

    [Range(0f, 1f)]
    public float positiveOpacity = 0.55f;

    [Range(0f, 1f)]
    public float negativeOpacity = 0.95f;

    [Range(0f, 1f)]
    public float gridOpacity = 0.2f;

    [Range(1f, 20f)]
    public float contourSpacingDecibels = 6f;

    [Range(0f, 1f)]
    public float contourIntensity = 0.6f;

    public float contourFlow = 0.4f;

    [Range(0f, 1f)]
    public float nodalIntensity = 0.9f;

    [Range(0f, 1f)]
    public float negativeHatch = 0.35f;

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

    [ColorUsage(true, true)]
    public Color negativeRimColor = new(0.75f, 0.25f, 1.0f, 1.0f);

    [ColorUsage(true, true)]
    public Color nodalColor = new(1.0f, 0.92f, 0.7f, 1.0f);

#if UNITY_EDITOR
    Mesh _sphere;
    int _sphereSubdivisions;
    Material _fieldMaterial;
    Material _occluderMaterial;
    MaterialPropertyBlock _properties;

    bool _hasFrame;
    Vector3 _listenerPosition;
    Quaternion _listenerRotation;

    static readonly int FIELD_ID = Shader.PropertyToID("_Field");
    static readonly int RECURRENCE_ID = Shader.PropertyToID("_Recurrence");
    static readonly int ORDER_ID = Shader.PropertyToID("_Order");
    static readonly int BASE_RADIUS_ID = Shader.PropertyToID("_BaseRadius");
    static readonly int DEFORM_SCALE_ID = Shader.PropertyToID("_DeformScale");
    static readonly int RADIUS_MODE_ID = Shader.PropertyToID("_RadiusMode");
    static readonly int SENSITIVITY_ID = Shader.PropertyToID("_Sensitivity");
    static readonly int RANGE_DECIBELS_ID = Shader.PropertyToID("_RangeDecibels");
    static readonly int FIELD_PEAK_ID = Shader.PropertyToID("_FieldPeak");

    static readonly int IDLE_OPACITY_ID = Shader.PropertyToID("_IdleOpacity");
    static readonly int POSITIVE_OPACITY_ID = Shader.PropertyToID("_PositiveOpacity");
    static readonly int NEGATIVE_OPACITY_ID = Shader.PropertyToID("_NegativeOpacity");
    static readonly int GRID_INTENSITY_ID = Shader.PropertyToID("_GridIntensity");
    static readonly int CONTOUR_SPACING_ID = Shader.PropertyToID("_ContourSpacing");
    static readonly int CONTOUR_INTENSITY_ID = Shader.PropertyToID("_ContourIntensity");
    static readonly int CONTOUR_FLOW_ID = Shader.PropertyToID("_ContourFlow");
    static readonly int NODAL_INTENSITY_ID = Shader.PropertyToID("_NodalIntensity");
    static readonly int HATCH_INTENSITY_ID = Shader.PropertyToID("_HatchIntensity");
    static readonly int HEAD_FORWARD_ID = Shader.PropertyToID("_HeadForward");

    static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
    static readonly int LOW_COLOR_ID = Shader.PropertyToID("_LowColor");
    static readonly int MID_COLOR_ID = Shader.PropertyToID("_MidColor");
    static readonly int HIGH_COLOR_ID = Shader.PropertyToID("_HighColor");
    static readonly int NEGATIVE_RIM_COLOR_ID = Shader.PropertyToID("_NegativeRimColor");
    static readonly int NODAL_COLOR_ID = Shader.PropertyToID("_NodalColor");

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

        Release(_fieldMaterial);
        Release(_occluderMaterial);
        Release(_sphere);
        Release(_speakerMaterial);
        Release(_speakerMesh);
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
        if (_properties == null)
        {
            _properties = new MaterialPropertyBlock();
            _properties.SetVectorArray(RECURRENCE_ID, BuildRecurrence());
        }

        if (!EnsureMaterial(ref _occluderMaterial, "Hidden/WyrmAudio/AmbisonicVisualizerOccluder"))
            return false;

        if (!EnsureMaterial(ref _fieldMaterial, "Hidden/WyrmAudio/AmbisonicVisualizer"))
            return false;

        if (_sphere == null || _sphereSubdivisions != subdivisions)
        {
            Release(_sphere);
            _sphere = BuildIcosphere(subdivisions);
            _sphereSubdivisions = subdivisions;
        }

        return true;
    }

    static bool EnsureMaterial(ref Material material, string shaderName)
    {
        if (material != null)
            return true;

        Shader shader = Shader.Find(shaderName);

        if (shader == null)
            return false;

        material = new Material(shader) { hideFlags = HideFlags.DontSave };
        return true;
    }

    static void Release(Object asset)
    {
        if (asset != null)
            DestroyImmediate(asset);
    }

    // csharpier-ignore
    void DrawField(int order)
    {
        _properties.SetVectorArray(FIELD_ID, _fieldUpload);
        _properties.SetFloat(ORDER_ID, order);
        _properties.SetFloat(BASE_RADIUS_ID, baseRadius);
        _properties.SetFloat(DEFORM_SCALE_ID, deformationScale);
        _properties.SetFloat(RADIUS_MODE_ID, radiusScale == AmbisonicRadiusScale.Decibel ? 1f : 0f);
        _properties.SetFloat(SENSITIVITY_ID, linearSensitivity);
        _properties.SetFloat(RANGE_DECIBELS_ID, rangeDecibels);
        _properties.SetFloat(FIELD_PEAK_ID, _fieldPeak.Value.w);

        _properties.SetFloat(IDLE_OPACITY_ID, idleOpacity);
        _properties.SetFloat(POSITIVE_OPACITY_ID, positiveOpacity);
        _properties.SetFloat(NEGATIVE_OPACITY_ID, negativeOpacity);
        _properties.SetFloat(GRID_INTENSITY_ID, gridOpacity);
        _properties.SetFloat(CONTOUR_SPACING_ID, contourSpacingDecibels);
        _properties.SetFloat(CONTOUR_INTENSITY_ID, contourIntensity);
        _properties.SetFloat(CONTOUR_FLOW_ID, contourFlow);
        _properties.SetFloat(NODAL_INTENSITY_ID, nodalIntensity);
        _properties.SetFloat(HATCH_INTENSITY_ID, negativeHatch);
        _properties.SetVector(HEAD_FORWARD_ID, _listenerRotation * Vector3.forward);

        _properties.SetColor(BASE_COLOR_ID, idleColor);
        _properties.SetColor(LOW_COLOR_ID, lowFreqColor);
        _properties.SetColor(MID_COLOR_ID, midFreqColor);
        _properties.SetColor(HIGH_COLOR_ID, highFreqColor);
        _properties.SetColor(NEGATIVE_RIM_COLOR_ID, negativeRimColor);
        _properties.SetColor(NODAL_COLOR_ID, nodalColor);

        var listenerCentered = Matrix4x4.Translate(_listenerPosition);
        Graphics.DrawMesh(_sphere, listenerCentered, _occluderMaterial, gameObject.layer, null, 0, _properties);
        Graphics.DrawMesh(_sphere, listenerCentered, _fieldMaterial, gameObject.layer, null, 0, _properties);
    }
#endif
}
