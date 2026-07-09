using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class WyrmAudioManager : MonoBehaviour
{
    private static WyrmAudioManager Instance;

    private AudioListener listener;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (Instance != null) return;

        var go = new GameObject("Wyrm Audio Manager");
        Instance = go.AddComponent<WyrmAudioManager>();
        go.AddComponent<WyrmRoomManager>();
        go.AddComponent<WyrmPoolController>();

        DontDestroyOnLoad(go);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeListener()
    {
        if (Instance.listener != null) return;
        Instance.listener = FindAnyObjectByType<AudioListener>();
    }

    public static void NotifyListenerChangeTo(AudioListener listener) => Instance.listener = listener;
    public static AudioListener GetAudioListener() => Instance.listener;
}