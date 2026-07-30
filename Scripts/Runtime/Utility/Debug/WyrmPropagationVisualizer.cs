using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class WyrmPropagationVisualizer : MonoBehaviour
{
    public bool enableVisualization = true;
    public float sensitivity = 25.0f;

    public float baseRadius = 2.0f;

    public float deformationScale = 1.5f;

    [Range(0f, 0.2f)]
    public float idleOpacity = 0.02f;

    [Range(0f, 1f)]
    public float gridOpacity = 0.25f;

    [Range(20, 100)]
    public int sphereResolution = 60;

    [ColorUsage(true, true)]
    public Color idleColor = new(0.02f, 0.1f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color lowFreqColor = new(1.0f, 0.05f, 0.0f, 1.0f);

    [ColorUsage(true, true)]
    public Color midFreqColor = new(0.1f, 1.0f, 0.3f, 1.0f);

    [ColorUsage(true, true)]
    public Color highFreqColor = new(0.0f, 0.8f, 1.0f, 1.0f);

    private Mesh _sphereMesh;
    private Material _shMaterial;
    private MaterialPropertyBlock _propBlock;

    private readonly Vector4[] _accumulatedSH = new Vector4[16];

    private static readonly int SH_COEFFS_ID = Shader.PropertyToID("_SHCoeffs");
    private static readonly int BASE_RADIUS_ID = Shader.PropertyToID("_BaseRadius");
    private static readonly int DEFORM_SCALE_ID = Shader.PropertyToID("_DeformScale");
    private static readonly int SENSITIVITY_ID = Shader.PropertyToID("_Sensitivity");
    private static readonly int IDLE_OPACITY_ID = Shader.PropertyToID("_IdleOpacity");
    private static readonly int GRID_INTENSITY_ID = Shader.PropertyToID("_GridIntensity");

    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
    private static readonly int LOW_COLOR_ID = Shader.PropertyToID("_LowColor");
    private static readonly int MID_COLOR_ID = Shader.PropertyToID("_MidColor");
    private static readonly int HIGH_COLOR_ID = Shader.PropertyToID("_HighColor");

    private void InitializeResources()
    {
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        if (_shMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/WyrmAudio/SHVisualizer");
            if (shader != null)
                _shMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (_sphereMesh == null)
        {
            _sphereMesh = GenerateHighResSphere(sphereResolution, sphereResolution);
            _sphereMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        }
    }

#if UNITY_EDITOR
    private void OnEnable() => InitializeResources();

    private void OnDisable()
    {
        if (_shMaterial != null)
            DestroyImmediate(_shMaterial);
        if (_sphereMesh != null)
            DestroyImmediate(_sphereMesh);
    }

    private void LateUpdate()
    {
        if (!enableVisualization || !Application.isPlaying)
            return;

        if (WyrmBaseSource.CompletelyInactive || WyrmListener.CompletelyInactive)
            return;

        if (_shMaterial == null || _sphereMesh == null)
            InitializeResources();

        int order = SteamAudio.SteamAudioSettings.Singleton != null ? SteamAudio.SteamAudioSettings.Singleton.realTimeAmbisonicOrder : 1;
        order = math.clamp(order, 0, 3);
        int numCoeffs = (order + 1) * (order + 1);

        for (int i = 0; i < 16; i++)
        {
            _accumulatedSH[i] = Vector4.zero;
        }

        for (int sourceIdx = 0; sourceIdx < WyrmBaseSource.ActiveCount; sourceIdx++)
        {
            if (!WyrmBaseSource.RegisteredInstances[sourceIdx].UseAmbisonics)
                return;
                
            int shOffset = sourceIdx * 16;

            float3 rawEq = WyrmBaseSource.CurrentPropagationEQ01[sourceIdx];
            float3 safeEq = math.max(rawEq, new float3(0.001f));

            var shNativeArray = WyrmBaseSource.TargetSHCoefficients;

            for (int c = 0; c < numCoeffs; c++)
            {
                float shValue = shNativeArray[shOffset + c];

                _accumulatedSH[c].w += shValue;
                _accumulatedSH[c].x += shValue * safeEq.x;
                _accumulatedSH[c].y += shValue * safeEq.y;
                _accumulatedSH[c].z += shValue * safeEq.z;
            }
        }

        _propBlock.SetVectorArray(SH_COEFFS_ID, _accumulatedSH);
        _propBlock.SetFloat(BASE_RADIUS_ID, baseRadius);
        _propBlock.SetFloat(DEFORM_SCALE_ID, deformationScale);
        _propBlock.SetFloat(SENSITIVITY_ID, sensitivity);
        _propBlock.SetFloat(IDLE_OPACITY_ID, idleOpacity);
        _propBlock.SetFloat(GRID_INTENSITY_ID, gridOpacity);

        _propBlock.SetColor(BASE_COLOR_ID, idleColor);
        _propBlock.SetColor(LOW_COLOR_ID, lowFreqColor);
        _propBlock.SetColor(MID_COLOR_ID, midFreqColor);
        _propBlock.SetColor(HIGH_COLOR_ID, highFreqColor);

        Graphics.DrawMesh(_sphereMesh, Matrix4x4.Translate(WyrmListener.ListenerPosition.Value), _shMaterial, gameObject.layer, null, 0, _propBlock);
    }
#endif

    private Mesh GenerateHighResSphere(int latLines, int longLines)
    {
        Mesh mesh = new() { name = "SH_RadarSphere", hideFlags = HideFlags.DontSave };

        int numVertices = (latLines + 1) * (longLines + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector3[] normals = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];

        int vIndex = 0;
        for (int lat = 0; lat <= latLines; lat++)
        {
            float v = (float)lat / latLines;
            float polar = v * Mathf.PI;
            float y = 0.5f * Mathf.Cos(polar);
            float r = 0.5f * Mathf.Sin(polar);

            for (int lon = 0; lon <= longLines; lon++)
            {
                float u = (float)lon / longLines;
                float azimuth = u * 2f * Mathf.PI;
                float x = r * Mathf.Cos(azimuth);
                float z = r * Mathf.Sin(azimuth);

                vertices[vIndex] = new Vector3(x, y, z);
                normals[vIndex] = vertices[vIndex].normalized;
                uvs[vIndex] = new Vector2(u, v);
                vIndex++;
            }
        }

        int[] triangles = new int[latLines * longLines * 6];
        int tIndex = 0;
        for (int lat = 0; lat < latLines; lat++)
        {
            for (int lon = 0; lon < longLines; lon++)
            {
                int current = lat * (longLines + 1) + lon;
                int next = current + 1;
                int currentBelow = current + (longLines + 1);
                int nextBelow = currentBelow + 1;

                triangles[tIndex++] = current;
                triangles[tIndex++] = currentBelow;
                triangles[tIndex++] = next;

                triangles[tIndex++] = next;
                triangles[tIndex++] = currentBelow;
                triangles[tIndex++] = nextBelow;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        return mesh;
    }
}
