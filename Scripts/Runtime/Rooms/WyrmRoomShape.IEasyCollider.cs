#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public sealed partial class WyrmRoomShape
{
    public Color Outline { get; set; } = Color.yellow.WithAlpha(0.8f);

    public Color Volume { get; set; } = Color.clear;

#if WYRMAUDIO_VISUALIZATION_ENABLED
    void OnDrawGizmos()
    {
        if (WyrmAudioManager.Listener != null)
        {
            var ListenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier.Value;
            if (ListenerRoomIdentifier != -1)
                Outline = ListenerRoomIdentifier == RoomIdentifier ? Color.cyan.WithAlpha(0.8f) : Color.yellow.WithAlpha(0.8f);
        }

        this.DrawVolumeGizmo(false, gameObject.name);
    }

    void OnDrawGizmosSelected()
    {
        this.DrawVolumeGizmo(true);

        if (_haltonBuffer == null)
            return;

        Gizmos.color = Color.grey;

        var matrix = transform.localToWorldMatrix;

        foreach (var p in _haltonBuffer)
            Gizmos.DrawSphere(matrix.MultiplyPoint3x4(p), .04f);
    }
#endif

    void OnValidate()
    {
        BoxCollider = this.EnsureReference(BoxCollider);

        this.ValidateCollider();

        HaltonSequence.GenerateBoxVolumeSamples(BoxCollider, sampleCount, _haltonBuffer);
    }
}
#endif
