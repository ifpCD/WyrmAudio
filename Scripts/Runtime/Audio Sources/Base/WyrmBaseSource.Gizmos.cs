#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class WyrmBaseSource
{
    internal void OnDrawGizmosSelected() => DrawGizmo();

    internal void DrawGizmo()
    {
        if (UseOcclusion && OcclusionMask != null)
            OcclusionMask.DrawOcclusionGizmo();

        if (!IsRegistered)
            return;

        Vector3 worldPos = Positions[SoAIndex];
        float currentOcc = CurrentOcclusion01[SoAIndex];

        float a = EasyColliderExtensions.GetGizmoOpacity(worldPos);
        WyrmAudioSettings.TextStyle.normal.textColor = WyrmAudioSettings.TextStyle.normal.textColor.WithAlpha(a);

        string dataDump = $"ID: {SoAIndex}\nOcc: {currentOcc:F2}\n";
        Handles.Label(worldPos, dataDump, WyrmAudioSettings.TextStyle);
    }
}
#endif
