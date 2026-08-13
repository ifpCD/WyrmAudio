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

    public static WyrmAudioSettings Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Resources.Load<WyrmAudioSettings>("WyrmAudioSettings");
            if (_instance != null) return _instance;

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
