#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    const float VIS_OCC_SAMPLE_RADIUS = 0.02f;

#if WYRMAUDIO_VISUALIZATION_ENABLED
    // void OnDrawGizmos() => DrawOcclusionGizmo();

    // void OnDrawGizmosSelected() => DrawOcclusionGizmo();
#endif

    public void DrawOcclusionGizmo()
    {
        var t = transform;
        var tPosition = t.position;
        var tLossyScale = t.lossyScale;

        if (_type == OcclusionMaskType.SphericalHalton)
        {
            Gizmos.color = WyrmColor.FaintWhite;
            Gizmos.DrawWireSphere(tPosition, Radius);
        }

        if (_sampleBuffer == null)
            return;

        var localToWorldNoRotation = Matrix4x4.TRS(tPosition, Quaternion.identity, tLossyScale);

        for (int i = 0; i < _sampleBuffer.Count; i++)
        {
            var sample = _sampleBuffer[i];

            var localPos = sample.LocalPosition;
            var worldPos = localToWorldNoRotation.MultiplyPoint3x4(localPos);

            Color SampleColor = Color.gray;

            if (sample.IsRegistered)
            {
                var isOccluded = WyrmOcclusionSample.IsOccluded[sample.SoAIndex];
                var isDiscarded = WyrmOcclusionSample.IsDiscarded[sample.SoAIndex];

                if (!isDiscarded && isOccluded)
                {
                    SampleColor = Color.red;
                }
                else if (!isDiscarded && !isOccluded)
                {
                    SampleColor = Color.green;
                }
            }

            Gizmos.color = SampleColor.WithAlpha(0.8f);
            Gizmos.DrawSphere(worldPos, VIS_OCC_SAMPLE_RADIUS);
            Gizmos.color = SampleColor.WithAlpha(0.2f);

            if (sample.Discardable)
                Gizmos.DrawLine(tPosition, worldPos);

            if (WyrmListener.CompletelyInactive)
                continue;

            Gizmos.DrawLine(worldPos, WyrmListener.ListenerPosition.Value);
        }
    }
}
#endif