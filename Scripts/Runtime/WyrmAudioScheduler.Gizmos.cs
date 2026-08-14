#if WYRMAUDIO_VISUALIZATION_ENABLED && UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public sealed partial class WyrmAudioScheduler
{
    [NonSerialized]
    private GUIStyle _textStyle;

    public GUIStyle TextStyle
    {
        get
        {
            _textStyle ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.white },
            };

            return _textStyle;
        }
    }

    void OnDrawGizmos()
    {
        if (WyrmBaseSource.CompletelyInactive)
            return;

        for (var i = 0; i < WyrmBaseSource.ActiveCount; i++)
        {
            Vector3 worldPos = WyrmBaseSource.Positions[i];
            float currentOcc = WyrmBaseSource.CurrentOcclusion01[i];

            float a = EasyColliderExtensions.GetGizmoOpacity(worldPos);
            TextStyle.normal.textColor = TextStyle.normal.textColor.WithAlpha(a);

            string dataDump = $"Occ: {currentOcc:F2}\n";
            Handles.Label(worldPos, dataDump, TextStyle);

            if (WyrmBaseSource.OcclusionMaskIndex[i] != -1)
                WyrmOcclusionMask.RegisteredInstances[i].DrawOcclusionGizmo();
        }
    }
}
#endif
