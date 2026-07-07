using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    private Transform _cachedTransform;

    internal TransformAccessArray SourceTransforms;
    internal TransformAccessArray TrackedTransforms;

    internal NativeArray<byte> IsTracking;

    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;
    internal NativeArray<float3> PropagationPositions;

    internal NativeArray<byte> SourceActiveStates;

    internal NativeArray<float> SourceMinDistances;
    internal NativeArray<float> SourceMaxDistances;

    internal NativeArray<float> OutputNormalizedRoomMixVolume;

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

        IsTracking = new(max, Allocator.Persistent);

        SourcePositions = new(max, Allocator.Persistent);
        TrackedPositions = new(max, Allocator.Persistent);
        PropagationPositions = new(max, Allocator.Persistent);

        SourceActiveStates = new(max, Allocator.Persistent);

        SourceMinDistances = new(max, Allocator.Persistent);
        SourceMaxDistances = new(max, Allocator.Persistent);

        OutputNormalizedRoomMixVolume = new(max, Allocator.Persistent);

        for (int i = 0; i < max; i++) CreatePooledAudioSource();
    }

    void Awake()
    {
        _cachedTransform = transform;
    }

    void OnDestroy()
    {
        DisposeNative();
    }

    void DisposeNative()
    {
        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();

        if (IsTracking.IsCreated) IsTracking.Dispose();

        if (SourcePositions.IsCreated) SourcePositions.Dispose();
        if (TrackedPositions.IsCreated) TrackedPositions.Dispose();
        if (PropagationPositions.IsCreated) PropagationPositions.Dispose();

        if (SourceActiveStates.IsCreated) SourceActiveStates.Dispose();

        if (SourceMinDistances.IsCreated) SourceMinDistances.Dispose();
        if (SourceMaxDistances.IsCreated) SourceMaxDistances.Dispose();

        if (OutputNormalizedRoomMixVolume.IsCreated) OutputNormalizedRoomMixVolume.Dispose();
    }

    void ReturnActiveSourceAtIndex(int index)
    {
        var source = _activeSources[index];
        int lastIndex = _activeCount - 1;

        _activeSources[index] = _activeSources[lastIndex];

        if (_activeSources[index] != null)
            _activeSources[index].ActiveIndex = index;

        _activeSources[lastIndex] = null;

        SourceTransforms.RemoveAtSwapBack(index);
        TrackedTransforms.RemoveAtSwapBack(index);

        SourcePositions[index] = SourcePositions[lastIndex];
        TrackedPositions[index] = TrackedPositions[lastIndex];

        IsTracking[index] = IsTracking[lastIndex];

        PropagationPositions[index] = TrackedPositions[lastIndex];

        SourceActiveStates[index] = SourceActiveStates[lastIndex];

        SourceMinDistances[index] = SourceMinDistances[lastIndex];
        SourceMaxDistances[index] = SourceMaxDistances[lastIndex];
        
        OutputNormalizedRoomMixVolume[index] = OutputNormalizedRoomMixVolume[lastIndex];

        _activeCount--;
        source.ActiveIndex = -1;
        source.Deactivate();
        source.IsBorrowed = false;

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

        pooledAudioSource.Initialize(this);

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}