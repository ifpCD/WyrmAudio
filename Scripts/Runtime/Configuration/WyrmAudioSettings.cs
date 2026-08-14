using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WyrmAudioSettings : ScriptableObject
{
    [Header("Sources")]
    [Min(1)]
    public int MaxActiveSources = 1024;

    public List<WyrmMixerGroupConfig> ActiveMixerConfigs = new();

    public List<Object> SoundBankFolders = new();

    [HideInInspector]
    public List<AbstractWyrmBank> RegisteredSoundBanks = new();

    [Header("Audio Rooms")]
    public int MaxActiveRooms = 1024;

    public LayerMask OcclusionMask = ~0;
    public LayerMask PropagationOcclusionMask = ~0;

    static WyrmAudioSettings _instance = null;

    [Header("Visualization")]
    public float CutoffDistance => FarDistance + 5f;

    [Range(1, 20)]
    public float NearDistance = 5f;

    [Range(5, 50)]
    public float FarDistance = 20;

    void OnValidate()
    {
        FarDistance = Mathf.Max(NearDistance + 0.5f, FarDistance);
    }

#if UNITY_EDITOR
    [System.NonSerialized]
    private static GUIStyle _gizmoTextStyle;

    public static GUIStyle TextStyle
    {
        get
        {
            _gizmoTextStyle ??= new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = Color.white },
            };

            return _gizmoTextStyle;
        }
    }
#endif

    public static WyrmAudioSettings Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            _instance = Resources.Load<WyrmAudioSettings>("WyrmAudioSettings");
            if (_instance != null)
                return _instance;

            _instance = CreateInstance<WyrmAudioSettings>();
            _instance.name = "WyrmAudioSettings";

#if WYRMAUDIO_DEVELOPMENT
            AssetDatabase.CreateAsset(_instance, "Assets/Plugins/WyrmAudio/Resources/WyrmAudioSettings.asset");
#elif UNITY_EDITOR
            AssetDatabase.CreateAsset(_instance, "Assets/Packages/WyrmAudio/Resources/WyrmAudioSettings.asset");
#endif

            return _instance;
        }
    }
}
