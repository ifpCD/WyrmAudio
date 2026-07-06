using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;



public partial class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    public TransformAccessArray SourceTransforms;
    public TransformAccessArray TrackedTransforms;

    public NativeArray<float3> SourcePositions;
    public NativeArray<float3> TrackedPositions;

    public NativeArray<byte> SourceActiveStates;
    public NativeArray<float> SourceMinDistances;
    public NativeArray<float> SourceMaxDistances;
    public NativeArray<float> OutputNormalizedRoomMixVolume;

    private IWyrmSource[] _availableSources;
    private int _availableCount;

    private IWyrmSource[] _activeSources;
    private int _activeCount;

    private int _totalCreated;

    public void Initialize(WyrmMixerGroupConfig config)
    {
        this.config = config;

        if (this.config.targetMixerGroup == null || this.config.WyrmAudioSourcePrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Incomplete Mixer Group Config");
#endif
            return;
        }

        int max = config.maxSize;

        _availableSources = new IWyrmSource[max];
        _activeSources = new IWyrmSource[max];

        SourceTransforms = new(max);
        TrackedTransforms = new(max);

        SourcePositions = new(max, Allocator.Persistent);
        TrackedPositions = new(max, Allocator.Persistent);
        SourceActiveStates = new(max, Allocator.Persistent);
        SourceMinDistances = new(max, Allocator.Persistent);
        SourceMaxDistances = new(max, Allocator.Persistent);
        OutputNormalizedRoomMixVolume = new(max, Allocator.Persistent);

        for (int i = 0; i < this.config.initialSize; i++)
            CreatePooledAudioSource();
    }

    public void Destroy() => DisposeNative();

    void DisposeNative()
    {
        SourcePositions.Dispose();
        TrackedPositions.Dispose();
        SourceActiveStates.Dispose();
        SourceMinDistances.Dispose();
        SourceMaxDistances.Dispose();
        OutputNormalizedRoomMixVolume.Dispose();

        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();
    }

    // internal void UpdateChildrenTransforms()
    // {
    //     if (config.isNonSpatial) return;

    //     var sources = _activeSources;
    //     for (int i = 0; i < _activeCount; i++)
    //     {
    //         var source = sources[i];
    //         var trackedTransform = source.TrackedTransform;

    //         if (trackedTransform == null)
    //         {
    //             source.TrackedTransform = null;
    //             continue;
    //         }

    //         trackedTransform.GetPositionAndRotation(out Vector3 currentPos, out Quaternion currentRot);

    //         if (source.CachedPosition != currentPos || source.CachedRotation != currentRot)
    //         {
    //             source.CachedPosition = currentPos;
    //             source.CachedRotation = currentRot;

    //             source.CachedTransform.SetPositionAndRotation(currentPos, currentRot);
    //         }
    //     }
    // }

    void ReturnActiveSourceAtIndex(int index)
    {
        var source = _activeSources[index];
        int lastIndex = _activeCount - 1;

        _activeSources[index] = _activeSources[lastIndex];

        if (_activeSources[index] != null)
        {
            _activeSources[index].ActiveIndex = index;
        }

        _activeSources[lastIndex] = null;

        SourcePositions[index] = SourcePositions[lastIndex];
        TrackedPositions[index] = TrackedPositions[lastIndex];
        SourceActiveStates[index] = SourceActiveStates[lastIndex];
        SourceMinDistances[index] = SourceMinDistances[lastIndex];
        SourceMaxDistances[index] = SourceMaxDistances[lastIndex];
        OutputNormalizedRoomMixVolume[index] = OutputNormalizedRoomMixVolume[lastIndex];

        SourceTransforms.RemoveAtSwapBack(index);
        TrackedTransforms.RemoveAtSwapBack(index);

        _activeCount--;
        source.Deactivate();

        _availableSources[_availableCount++] = source;
    }

    void CreatePooledAudioSource()
    {
        if (_totalCreated >= config.maxSize) return;

        GameObject newPooledAudioSourceGO = Instantiate(config.WyrmAudioSourcePrefab, transform);
        if (!newPooledAudioSourceGO.TryGetComponent(out IWyrmSource pooledAudioSource))
        {
#if UNITY_EDITOR
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IWyrmSource type component");
#endif
            return;
        }

        pooledAudioSource.Initialize(config);

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}