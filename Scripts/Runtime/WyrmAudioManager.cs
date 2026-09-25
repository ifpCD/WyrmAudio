using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[DisallowMultipleComponent]
[NoAutoStaticsCleanup]
public class WyrmAudioManager : MonoBehaviour
{
    private static WyrmAudioManager Instance;

    public static WyrmListener Listener { get; internal set; }

    public static void NotifyListenerModified(WyrmListener listener) => Listener = listener;

    public static void NotifyCameraModified(Camera camera) { }

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
}
