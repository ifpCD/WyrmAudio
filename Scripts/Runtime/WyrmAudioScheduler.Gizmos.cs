#if WYRMAUDIO_VISUALIZATION_ENABLED && UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public sealed partial class WyrmAudioScheduler
{
    void OnDrawGizmos()
    {
        if (WyrmBaseSource.CompletelyInactive)
            return;

        var textStyle = WyrmAudioSettings.TextStyle;

        for (var i = 0; i < WyrmBaseSource.ActiveCount; i++)
        {
            WyrmBaseSource.RegisteredInstances[i].DrawGizmo();
        }
    }
}
#endif
