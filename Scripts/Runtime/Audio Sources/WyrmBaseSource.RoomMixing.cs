using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    // Placeholder logic for now
    void OnAudioFilterRead(float[] data, int channels)
    {
        // float gain = Manager.OutputNormalizedRoomMixVolume[ActiveIndex];

        // for (int i = 0; i < data.Length; i++)
        // {
        //     data[i] *= gain;
        // }
    }
}