using UnityEngine;

[DisallowMultipleComponent]
public class WyrmAudioManager : MonoBehaviour
{
    private static WyrmAudioManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (instance != null)
            return;

        var go = new GameObject("Wyrm Audio Manager");
        
        instance = go.AddComponent<WyrmAudioManager>();
        go.AddComponent<WyrmPool>();

        DontDestroyOnLoad(go);
    }
}