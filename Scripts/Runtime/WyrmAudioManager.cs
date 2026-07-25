using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class WyrmAudioManager : MonoBehaviour
{
    private static WyrmAudioManager Instance;

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
