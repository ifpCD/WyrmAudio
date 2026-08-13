using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
            Gizmos.color = Color.antiqueWhite;
            Gizmos.DrawWireSphere(cachedTransformPosition, Radius);
        }

        if (_occlusionSamples == null)
            return;

        var matrix = Matrix4x4.TRS(cachedTransformPosition, Quaternion.identity, cachedTransform.lossyScale);

        for (int i = 0; i < _occlusionSamples.Count; i++)
        {
            var sample = _occlusionSamples[i];

            var localPos = sample.LocalPosition;
            var worldPos = matrix.MultiplyPoint3x4(localPos);

            if (sample.IsRegistered)
            {
                var isOccluded = OcclusionSample.IsOccluded[i].ToBool();
                var IsDiscarded = OcclusionSample.IsDiscarded[i].ToBool();

                if (sample.Discardable && IsDiscarded)
                    Gizmos.color = Color.grey;
                else if (isOccluded)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
            }
            else
                Gizmos.color = Color.grey;

            Gizmos.DrawSphere(worldPos, VIS_OCC_SAMPLE_RADIUS);

            if (sample.Discardable)
                Gizmos.DrawLine(cachedTransformPosition, worldPos);

            if (WyrmListener.CompletelyInactive)
                continue;

            Gizmos.DrawLine(localPos, WyrmListener.ListenerPosition.Value);
        }
    }
}
