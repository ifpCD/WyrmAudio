using UnityEngine;

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    const float VIS_OCC_SAMPLE_RADIUS = 0.02f;

    void OnDrawGizmosSelected() => DrawOcclusionGizmo();

    public void DrawOcclusionGizmo()
    {
        var cachedTransform = transform;
        var cachedTransformPosition = cachedTransform.position;

        if (_type == OcclusionMaskType.SphericalHalton)
        {
            Gizmos.color = WyrmColor.FaintWhite;
            Gizmos.DrawWireSphere(cachedTransformPosition, Radius);
        }

        if (_sampleBuffer == null)
            return;

        var matrix = Matrix4x4.TRS(cachedTransformPosition, Quaternion.identity, cachedTransform.lossyScale);

        for (int i = 0; i < _sampleBuffer.Count; i++)
        {
            var sample = _sampleBuffer[i];

            var localPos = sample.LocalPosition;
            var worldPos = matrix.MultiplyPoint3x4(localPos);

            Color SampleColor = WyrmColor.Gray;
            Color LineColor = WyrmColor.FaintGray;

            if (sample.IsRegistered)
            {
                var isOccluded = OcclusionSample.IsOccluded[i].ToBool();
                var isDiscarded = OcclusionSample.IsDiscarded[i].ToBool();

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
                Gizmos.DrawLine(cachedTransformPosition, worldPos);

            if (WyrmListener.CompletelyInactive)
                continue;

            Gizmos.DrawLine(worldPos, WyrmListener.ListenerPosition.Value);
        }
    }
}
