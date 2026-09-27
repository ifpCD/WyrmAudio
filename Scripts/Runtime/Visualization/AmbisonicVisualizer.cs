using UnityEngine;
using UnityEngine.Audio;

public enum AmbisonicVisualization : byte
{
    Encoded,
    Rendered,
}

// Sum of every audible source's banded field around the listener, world-aligned. Rendered applies Phonon's per-band
// max-rE decoder weighting; Encoded shows the raw coefficients, truncation ringing (negative lobes) included.
[DefaultExecutionOrder(200)]
public sealed partial class AmbisonicVisualizer : MonoBehaviour
{
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

    [ColorUsage(true, true)]
    public Color idleColor = new(0.02f, 0.1f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color lowFreqColor = new(1.0f, 0.05f, 0.0f, 1.0f);

    [ColorUsage(true, true)]
    public Color midFreqColor = new(0.1f, 1.0f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color highFreqColor = new(0.0f, 0.8f, 1.0f, 1.0f);

    [ColorUsage(true, true)]
    public Color negativeLobeColor = new(0.55f, 0.0f, 1.0f, 1.0f);

#if UNITY_EDITOR
    Mesh _sphere;
    int _sphereSubdivisions;
    Material _material;
    MaterialPropertyBlock _properties;

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
    static readonly int NEGATIVE_COLOR_ID = Shader.PropertyToID("_NegativeColor");

    void OnEnable() => AllocateField();

    void OnDisable()
    {
        DeallocateField();

        if (_material != null)
            DestroyImmediate(_material);

        if (_sphere != null)
            DestroyImmediate(_sphere);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || WyrmBaseSource.CompletelyInactive || WyrmAudioManager.Listener == null)
            return;

        if (!EnsureResources())
            return;

        int order = AccumulateField();
        Draw(order);
    }

    bool EnsureResources()
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

    void Draw(int order)
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
        _properties.SetColor(NEGATIVE_COLOR_ID, negativeLobeColor);

        Graphics.DrawMesh(_sphere, Matrix4x4.Translate(WyrmListener.ListenerPosition.Value), _material, gameObject.layer, null, 0, _properties);
    }
#endif
}
