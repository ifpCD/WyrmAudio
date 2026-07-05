using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WyrmAudioSettings : ScriptableObject
{
    public List<WyrmMixerGroupConfig> ActiveMixerConfigs = new();

    [HideInInspector]
    public List<WyrmSoundBank> RegisteredSoundBanks = new();

    public LayerMask roomLayer;

    static WyrmAudioSettings _instance = null;

    public static WyrmAudioSettings Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Resources.Load<WyrmAudioSettings>("Wyrm Audio Settings");
            if (_instance != null) return _instance;

            _instance = CreateInstance<WyrmAudioSettings>();
            _instance.name = "Wyrm Audio Settings";

#if WYRMAUDIO_DEVELOPMENT
            AssetDatabase.CreateAsset(_instance, "Assets/Plugins/WyrmAudio/Resources/WyrmAudioSettings.asset");
#elif UNITY_EDITOR
            AssetDatabase.CreateAsset(_instance, "Assets/Packages/WyrmAudio/Resources/WyrmAudioSettings.asset");
#endif

            return _instance;
        }
    }
}