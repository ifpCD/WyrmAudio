using UnityEngine;

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    const float VIS_OCC_SAMPLE_RADIUS = 0.02f;

    void OnDrawGizmosSelected() => DrawOcclusionGizmo();

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

            Color SampleColor = WyrmColor.Gray;
            Color LineColor = WyrmColor.FaintGray;

            if (sample.IsRegistered)
            {
                var isOccluded = WyrmOcclusionSample.IsOccluded[i];
                var isDiscarded = WyrmOcclusionSample.IsDiscarded[i];

                if (!isDiscarded && isOccluded)
                {
                    SampleColor = WyrmColor.Red;
                    LineColor = WyrmColor.FaintRed;
                }
                else if (!isDiscarded && !isOccluded)
                {
                    SampleColor = WyrmColor.Green;
                    LineColor = WyrmColor.FaintGreen;
                }
            }

            Gizmos.color = SampleColor;
            Gizmos.DrawSphere(worldPos, VIS_OCC_SAMPLE_RADIUS);

            Gizmos.color = LineColor;

            if (sample.Discardable)
                Gizmos.DrawLine(tPosition, worldPos);

            if (WyrmListener.CompletelyInactive)
                continue;

            Gizmos.DrawLine(worldPos, WyrmListener.ListenerPosition.Value);
        }
    }
}
