using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[DisallowMultipleComponent]
[NoAutoStaticsCleanup]
public sealed partial class WyrmAmbisonicGenerator : AmbiMonoBehaviour<WyrmAmbisonicGenerator>
{
    protected override int AllocatedCapacity => HC.MAX_AMBISONIC_CONTRIBUTORS;

    [SerializeField]
    AmbisonicGeneratorType _type;

    [SerializeField]
    [Range(0f, 1f)]
    float _volume = 1f;

    [SerializeField]
    Vector3Int _ambisonicOrders = new(HC.MAX_AMBISONIC_ORDER, HC.MAX_AMBISONIC_ORDER, HC.MAX_AMBISONIC_ORDER);

    [SerializeField]
    Vector3 _bandVolumes = Vector3.one;

    [SerializeField]
    Vector3 _bandSpreadDegrees;

    [SerializeField]
    [Range(0f, 180f)]
    float _horizontalSpreadDegrees;

    [SerializeField]
    SimpleAmbisonicType _simpleType = SimpleAmbisonicType.Positional;

    [SerializeField]
    MeshFilter _meshTarget;

    [SerializeField]
    bool _meshVertexColorBands;

    public AmbisonicGeneratorType Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;

            DetachKind();
            _type = value;

            if (IsRegistered)
                AttachKind();
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Mathf.Clamp01(value);

            if (IsRegistered)
                BandGains[SoAIndex] = BandGain;
        }
    }

    // per band (x low, y mid, z high), each in [0, HC.MAX_AMBISONIC_ORDER]
    public Vector3Int AmbisonicOrders
    {
        get => _ambisonicOrders;
        set
        {
            int3 orders = ClampedOrders(value);
            _ambisonicOrders = new Vector3Int(orders.x, orders.y, orders.z);

            if (IsRegistered)
                BandOrders[SoAIndex] = orders;
        }
    }

    public Vector3 BandVolumes
    {
        get => _bandVolumes;
        set
        {
            _bandVolumes = value;

            if (IsRegistered)
                BandGains[SoAIndex] = BandGain;
        }
    }

    public Vector3 BandSpreadDegrees
    {
        get => _bandSpreadDegrees;
        set
        {
            _bandSpreadDegrees = value;

            if (IsRegistered)
                BandSpreads[SoAIndex] = BandSpreadRadians;
        }
    }

    public float HorizontalSpreadDegrees
    {
        get => _horizontalSpreadDegrees;
        set
        {
            _horizontalSpreadDegrees = value;

            if (IsRegistered)
                HorizontalSpreads[SoAIndex] = math.radians(value);
        }
    }

    public SimpleAmbisonicType SimpleType
    {
        get => _simpleType;
        set
        {
            _simpleType = value;
            _simpleRow?.SyncType();
        }
    }

    public MeshFilter MeshTarget
    {
        get => _meshTarget;
        set
        {
            _meshTarget = value;

            if (IsRegistered)
                ReattachKind();
        }
    }

    public bool MeshVertexColorBands
    {
        get => _meshVertexColorBands;
        set
        {
            _meshVertexColorBands = value;

            if (IsRegistered)
                ReattachKind();
        }
    }

    static int3 ClampedOrders(Vector3Int orders) => math.clamp(new int3(orders.x, orders.y, orders.z), 0, HC.MAX_AMBISONIC_ORDER);

    float3 BandGain => _volume * math.saturate((float3)_bandVolumes);
    float3 BandSpreadRadians => math.radians((float3)_bandSpreadDegrees);

    void OnEnable()
    {
        Register();
        AttachKind();
    }

    void OnDisable()
    {
        DetachKind();
        Deregister();
    }

    void OnValidate()
    {
        int3 orders = ClampedOrders(_ambisonicOrders);
        _ambisonicOrders = new Vector3Int(orders.x, orders.y, orders.z);

        if (!IsRegistered)
            return;

        SyncShaping();
        _simpleRow?.SyncType();

        if (AttachmentIsStale)
            ReattachKind();
    }
}
