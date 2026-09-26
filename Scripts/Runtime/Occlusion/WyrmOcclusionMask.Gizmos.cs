#if UNITY_EDITOR
using UnityEngine;

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    const float VIS_OCC_SAMPLE_RADIUS = 0.02f;

#if WYRMAUDIO_VISUALIZATION_ENABLED && UNITY_EDITOR
    void OnDrawGizmosSelected() => DrawOcclusionGizmo();
#endif

    public void DrawOcclusionGizmo()
    {
        if (WyrmAudioManager.Listener == null)
            return;

        if (_generatedSampleData == null)
            return;

        var t = transform;
        var tPosition = t.position;
        var tLossyScale = t.lossyScale;

        if (_type == OcclusionMaskType.SphericalHalton)
        {
            Gizmos.color = WyrmColor.FaintWhite;
            Gizmos.DrawWireSphere(tPosition, Radius);
        }

        var localToWorldNoRotation = Matrix4x4.TRS(tPosition, Quaternion.identity, tLossyScale);
        int chunkStartOffset = SoAIndex * HC.MAX_OCC_SAMPLES_PER_MASK;

        for (int i = 0; i < _generatedSampleData.Count; i++)
        {
            var sample = _generatedSampleData[i];

            var localPos = sample.LocalPosition;
            var worldPos = localToWorldNoRotation.MultiplyPoint3x4(localPos);

            Color SampleColor = Color.gray;

            if (IsRegistered)
            {
                int nativeIdx = chunkStartOffset + i;
                var isOccluded = SampleIsOccluded[nativeIdx];
                var isDiscarded = SampleIsDiscarded[nativeIdx];

                if (!isDiscarded && isOccluded)
                    SampleColor = Color.red;
                else if (!isDiscarded && !isOccluded)
                    SampleColor = Color.green;
            }

            Gizmos.color = SampleColor.WithAlpha(0.8f);
            Gizmos.DrawSphere(worldPos, VIS_OCC_SAMPLE_RADIUS);
            Gizmos.color = SampleColor.WithAlpha(0.2f);

            if (sample.Discardable)
                Gizmos.DrawLine(tPosition, worldPos);

            Gizmos.DrawLine(worldPos, WyrmListener.ListenerPosition.Value);
        }
    }
}
#endif
