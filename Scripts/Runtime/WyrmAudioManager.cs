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
        if (Instance != null)
            return;

        var go = new GameObject("Wyrm Audio Manager");
        Instance = go.AddComponent<WyrmAudioManager>();
        go.AddComponent<WyrmPoolController>();
        go.AddComponent<WyrmAudioScheduler>();

        DontDestroyOnLoad(go);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeListener()
    {
        if (Instance.listener != null)
            return;

        NotifyListenerChangeTo(FindAnyObjectByType<AudioListener>());
    }

    public static void NotifyListenerChangeTo(AudioListener listener)
    {
        if (Instance.listener == listener)
            return;

        if (
            Instance.listener != null
            && Instance.listener.TryGetComponent<WyrmListener>(out WyrmListener previousListener)
        )
            previousListener.enabled = false;

        Instance.listener = listener;

        if (listener == null)
            return;

        if (!listener.TryGetComponent<WyrmListener>(out WyrmListener nextListener))
            nextListener = listener.gameObject.AddComponent<WyrmListener>();

        nextListener.enabled = true;
    }

    public static AudioListener GetAudioListener() => Instance.listener;
}
