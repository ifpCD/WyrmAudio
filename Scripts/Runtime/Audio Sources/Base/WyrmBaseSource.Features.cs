using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Audio;

public partial class WyrmBaseSource : AmbiMonoBehaviour<WyrmBaseSource>, IWyrmSource
{

    [Header("Spatial Features")]
    [field: SerializeField]
    public virtual bool UseDirect { get; set; } = false;

    [field: SerializeField]
    public virtual bool UseReflections { get; set; } = false;

    [field: SerializeField]
    public virtual bool UseAmbisonics { get; set; } = true;

    [field: SerializeField]
    public virtual bool UseOcclusion { get; set; } = true;
}
