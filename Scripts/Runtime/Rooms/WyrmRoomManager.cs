using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public partial class WyrmRoomManager : MonoBehaviour
{
    public static WyrmRoomManager Instance { get; private set; }

    [Header("Settings")]
    public LayerMask OcclusionLayerMask = ~0;

    public NativeReference<int> ListenerRoomIdentifier;
    [HideInInspector] public float3 ListenerPosition { get; private set; }

    [HideInInspector] public NativeArray<RoomData> Rooms;
    [HideInInspector] public NativeArray<PortalData> Portals;
    [HideInInspector] public NativeArray<RoomAcousticMap> AcousticMap;

    [HideInInspector] public NativeParallelMultiHashMap<int, int> RoomToPortals;

    private WyrmRoom[] _roomRefs;
    private WyrmPortal[] _portalRefs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        // Restore Singleton instance if lost during Domain Reload
        if (Instance == null) 
        {
            Instance = this;
        }

        if (Instance == this)
        {
            InitializeGraph();
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            DisposeCollections();
        }
    }

    private void OnDestroy()
    {
        DisposeCollections();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeGraph()
    {
        // Clean up first just in case to prevent leaks
        DisposeCollections();

        _roomRefs = FindObjectsByType<WyrmRoom>(FindObjectsSortMode.None);
        _portalRefs = FindObjectsByType<WyrmPortal>(FindObjectsSortMode.None);

        ListenerRoomIdentifier = new NativeReference<int>(allocator: Allocator.Persistent);
        Rooms = new NativeArray<RoomData>(_roomRefs.Length, Allocator.Persistent);
        Portals = new NativeArray<PortalData>(_portalRefs.Length, Allocator.Persistent);
        AcousticMap = new NativeArray<RoomAcousticMap>(_roomRefs.Length, Allocator.Persistent);

        RoomToPortals = new NativeParallelMultiHashMap<int, int>(_roomRefs.Length * 32, Allocator.Persistent);

        PopulateRooms();
        PopulatePortalsAndRelations();
    }

    private void DisposeCollections()
    {
        if (ListenerRoomIdentifier.IsCreated) ListenerRoomIdentifier.Dispose();
        if (Rooms.IsCreated) Rooms.Dispose();
        if (Portals.IsCreated) Portals.Dispose();
        if (AcousticMap.IsCreated) AcousticMap.Dispose();
        if (RoomToPortals.IsCreated) RoomToPortals.Dispose();
    }

    public JobHandle ScheduleEffects()
    {
        // Domain reload null checks
        if (WyrmPoolController.Instance == null || WyrmPoolController.Instance.ActiveCount == 0)
            return default;

        // Ensure collections survived / are initialized
        if (!Rooms.IsCreated || !Portals.IsCreated)
            return default;

        var listener = WyrmAudioManager.GetAudioListener();
        if (listener == null)
            return default;

        ListenerPosition = listener.transform.position;

        var locateListenerJob = new LocateListenerJob
        {
            ListenerPosition = ListenerPosition,
            Rooms = Rooms,
            ListenerRoomIdentifier = ListenerRoomIdentifier
        };
        JobHandle locateListenerHandle = locateListenerJob.Schedule();

        var calculateMapJob = new CalculateAcousticMapJob
        {
            ListenerRoomIdentifier = ListenerRoomIdentifier,
            Rooms = Rooms,
            Portals = Portals,
            RoomToPortals = RoomToPortals,
            AcousticMap = AcousticMap
        };
        JobHandle calculateMapHandle = calculateMapJob.Schedule(locateListenerHandle);

        var locateSourcesJob = new LocateSourcesJob
        {
            SourcePositions = WyrmPoolController.Instance.SourcePositions,
            Rooms = Rooms,
            SourceRoomIdentifiers = WyrmPoolController.Instance.SourceRoomIdentifiers
        };
        JobHandle locateSourcesHandle = locateSourcesJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16);

        JobHandle propagationDeps = JobHandle.CombineDependencies(calculateMapHandle, locateSourcesHandle);

        var resolvePropagationJob = new ResolvePropagationJob
        {
            SourceRoomIdentifiers = WyrmPoolController.Instance.SourceRoomIdentifiers,
            SourcePositions = WyrmPoolController.Instance.SourcePositions,
            AcousticMap = AcousticMap,
            Portals = Portals,
            ListenerPosition = ListenerPosition,
            PropagationDirections = WyrmPoolController.Instance.PropagationDirections,
            PropagationDistances = WyrmPoolController.Instance.PropagationDistances,
            PropagationPathEQs = WyrmPoolController.Instance.TargetPropagationEQ01,
        };
        JobHandle resolvePropagationHandle = resolvePropagationJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, propagationDeps);

        var calculateSHJob = new CalculateSHCoefficientsJob
        {
            Directions = WyrmPoolController.Instance.PropagationDirections,
            Distances = WyrmPoolController.Instance.PropagationDistances,
            AmbisonicOrder = SteamAudio.SteamAudioSettings.Singleton.realTimeAmbisonicOrder,
            SHCoeffs = WyrmPoolController.Instance.PropagationSHCoeffOutputs
        };
        JobHandle shHandle = calculateSHJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, resolvePropagationHandle);

        var prepareRaycastsJob = new PrepareOcclusionRaycastsJob
        {
            SourcePositions = WyrmPoolController.Instance.SourcePositions,
            ListenerPosition = ListenerPosition,
            LayerMask = WyrmAudioSettings.Instance.DefaultMask,
            RaycastCommands = WyrmPoolController.Instance.OcclusionCommands
        };
        JobHandle prepareRaycastsHandle = prepareRaycastsJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
            WyrmPoolController.Instance.OcclusionCommands,
            WyrmPoolController.Instance.OcclusionHitResults,
            16, prepareRaycastsHandle);

        var resolveOcclusionJob = new ResolveOcclusionJob
        {
            RaycastHits = WyrmPoolController.Instance.OcclusionHitResults,
            SourceOcclusions = WyrmPoolController.Instance.TargetOcclusion01
        };
        JobHandle resolveOcclusionHandle = resolveOcclusionJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, raycastHandle);

        return JobHandle.CombineDependencies(shHandle, resolveOcclusionHandle);
    }

    private void PopulateRooms()
    {
        for (int i = 0; i < _roomRefs.Length; i++)
        {
            var room = _roomRefs[i];
            Rooms[i] = new RoomData
            {
                roomIdentifier = room.RoomIdentifier,
                center = room.transform.TransformPoint(room.BoxCollider.center),
                extents = Vector3.Scale(room.BoxCollider.size, room.transform.lossyScale) * 0.5f,
                rotation = room.transform.rotation
            };
        }
    }

    private void PopulatePortalsAndRelations()
    {
        for (int i = 0; i < _portalRefs.Length; i++)
        {
            var portal = _portalRefs[i];

            int indexA = portal.RoomA != null ? System.Array.IndexOf(_roomRefs, portal.RoomA) : -1;
            int indexB = portal.RoomB != null ? System.Array.IndexOf(_roomRefs, portal.RoomB) : -1;

            Portals[i] = new PortalData
            {
                center = portal.transform.TransformPoint(portal.BoxCollider.center),
                extents = Vector3.Scale(portal.BoxCollider.size, portal.transform.lossyScale) * 0.5f,
                forward = portal.transform.forward,
                rotation = portal.transform.rotation,
                roomA = indexA,
                roomB = indexB,
                openness = portal.Openness
            };

            if (indexA != -1) RoomToPortals.Add(indexA, i);
            if (indexB != -1) RoomToPortals.Add(indexB, i);
        }
    }

    private void Update()
    {
        if (_portalRefs == null || !Portals.IsCreated) 
            return;

        for (int i = 0; i < _portalRefs.Length; i++)
        {
            if (_portalRefs[i] == null) continue;

            var pData = Portals[i];
            float currentOpenness = _portalRefs[i].Openness;

            if (math.abs(pData.openness - currentOpenness) > 0.001f)
            {
                pData.openness = currentOpenness;
                Portals[i] = pData;
            }
        }
    }
}