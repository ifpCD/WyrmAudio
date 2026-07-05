using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public class WyrmAudioManager : MonoBehaviour
{
    private static WyrmAudioManager instance;

    private AudioListener listener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (instance != null) return;
        WyrmPool.Dispose();

        var go = new GameObject("Wyrm Audio Manager");

        instance = go.AddComponent<WyrmAudioManager>();
        go.AddComponent<WyrmPool>();

        DontDestroyOnLoad(go);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeListener()
    {
        if (instance.listener != null) return;

        instance.listener = FindAnyObjectByType<AudioListener>();
    }

    public static void NotifyListenerChangeTo(AudioListener listener) => instance.listener = listener;
    public static AudioListener GetAudioListener() => instance.listener;
}