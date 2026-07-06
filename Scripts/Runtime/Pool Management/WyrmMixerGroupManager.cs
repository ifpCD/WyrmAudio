using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    internal TransformAccessArray SourceTransforms;

    internal TransformAccessArray TrackedTransforms;

    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;
    internal NativeArray<float3> PropagationPositions;

    internal NativeArray<byte> SourceActiveStates;
    internal NativeArray<float> SourceMinDistances;
    internal NativeArray<float> SourceMaxDistances;

    // Room mixing
    internal NativeArray<float> OutputNormalizedRoomMixVolume;

    private Queue<TransformUpdate> _pendingTransformUpdates;

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

        _pendingTransformUpdates = new(1000); // todo
        _availableSources = new IWyrmSource[max];
        _activeSources = new IWyrmSource[max];

        SourceTransforms = new(max);
        TrackedTransforms = new(max);

        SourcePositions = new(max, Allocator.Persistent);
        TrackedPositions = new(max, Allocator.Persistent);
        PropagationPositions = new(max, Allocator.Persistent);

        SourceActiveStates = new(max, Allocator.Persistent);
        SourceMinDistances = new(max, Allocator.Persistent);
        SourceMaxDistances = new(max, Allocator.Persistent);

        OutputNormalizedRoomMixVolume = new(max, Allocator.Persistent);

        for (int i = 0; i < max; i++) CreatePooledAudioSource();
    }

    public void Destroy() => DisposeNative();

    void DisposeNative()
    {
        SourcePositions.Dispose();
        TrackedPositions.Dispose();
        PropagationPositions.Dispose();
        SourceActiveStates.Dispose();
        SourceMinDistances.Dispose();
        SourceMaxDistances.Dispose();
        OutputNormalizedRoomMixVolume.Dispose();

        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();
    }

    void ReturnActiveSourceAtIndex(int index)
    {
        var source = _activeSources[index];
        int lastIndex = _activeCount - 1;

        _activeSources[index] = _activeSources[lastIndex];

        if (_activeSources[index] != null)
            _activeSources[index].ActiveIndex = index;

        _activeSources[lastIndex] = null;

        SourcePositions[index] = SourcePositions[lastIndex];
        TrackedPositions[index] = TrackedPositions[lastIndex];
        PropagationPositions[index] = TrackedPositions[lastIndex];

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

        pooledAudioSource.Initialize(this);

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}