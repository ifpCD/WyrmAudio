using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

// Room and Portal Graph must initialize before any other audio managers
[DefaultExecutionOrder(-150)]
internal partial class GraphManager : MonoBehaviour
{
    internal static GraphManager Instance { get; private set; }

    private WyrmRoomShape[] _roomShapes = Array.Empty<WyrmRoomShape>();
    private WyrmPortal[] _portals = Array.Empty<WyrmPortal>();

    bool _dirty = true;

    private void Awake()
    {
        CollectGraphObjects();
        Allocate();

        InformChildrenGraphObjects();
        BuildGraph();

        Instance = this;
    }

    void LateUpdate()
    {
        if (_dirty)
            BuildGraph();
    }

    void OnDestroy() => Deallocate();

    void BuildGraph()
    {
        _dirty = false;
    }

    private void CollectGraphObjects()
    {
        _roomShapes = FindObjectsByType<WyrmRoomShape>();
        _portals = FindObjectsByType<WyrmPortal>();
    }

    public void InformChildrenGraphObjects()
    {
        for (int i = 0; i < _roomShapes.Length; i++)
            _roomShapes[i].InformOfRegistration(this, i);

        for (int i = 0; i < _portals.Length; i++)
            _portals[i].InformOfRegistration(this, i);
    }
}
