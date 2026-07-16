using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-150)]
internal partial class PropagationManager : MonoBehaviour
{
    internal static PropagationManager Instance { get; private set; }


    private void Awake()
    {
        Allocate();

        Instance = this;
    }

    void OnDestroy() => Deallocate();

    // csharpier-ignore
    void Allocate()
    {

    }

    // csharpier-ignore
    void Deallocate()
    {

    }
}
