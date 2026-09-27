#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed partial class AmbisonicVisualizer
{
    static readonly string[] ENERGY_VECTOR_NAMES = { "Low", "Mid", "High", "Broadband" };

    readonly int[] _loudestSpeakers = new int[16];

    // Gerzon energy vectors: direction is the perceived source direction, acos(|rE|) its angular spread.
    void OnDrawGizmos()
    {
        if (!_hasFrame)
            return;

        GUIStyle style = WyrmAudioSettings.TextStyle;
        style.normal.textColor = Color.white;

        Handles.color = new Color(0.35f, 0.6f, 1f, 0.9f);
        Handles.DrawLine(_listenerPosition, _listenerPosition + _listenerRotation * Vector3.forward * (0.5f * speakerRadius), 3f);

        int3 orders = _fieldOrders.Value;
        string header = $"{visualization}  orders {orders.x}/{orders.y}/{orders.z}  sources {_contributingSources}  peak feed {Decibels(_speakerPeak):F1} dB";
        Handles.Label(_listenerPosition + Vector3.up * (speakerRadius + 0.6f), header, style);

        float4 peak = _fieldPeak.Value;

        if (peak.w > 0f)
            DrawPeak(peak, style);

        if (showEnergyVectors)
            DrawEnergyVectors(style);

        if (showVirtualSpeakers && labeledSpeakers > 0 && _speakerPeak > 0f)
            DrawLoudestSpeakers(style);
    }

    // the balloon's apex in the head frame: where the field places the sound
    void DrawPeak(float4 peak, GUIStyle style)
    {
        float level = radiusScale == AmbisonicRadiusScale.Decibel ? 1f : math.saturate(math.pow(peak.w * linearSensitivity, 0.8f));
        Vector3 tip = _listenerPosition + (Vector3)(peak.xyz * (baseRadius + level * deformationScale));
        Vector3 head = Quaternion.Inverse(_listenerRotation) * (Vector3)peak.xyz;

        Handles.color = nodalColor;
        Handles.DrawDottedLine(_listenerPosition, tip, 4f);
        Handles.Label(tip, $"peak  az {math.degrees(math.atan2(head.x, head.z)):F1}°  el {math.degrees(math.asin(math.clamp(head.y, -1f, 1f))):F1}°", style);
    }

    void DrawEnergyVectors(GUIStyle style)
    {
        Color[] colors = { lowFreqColor, midFreqColor, highFreqColor, Color.white };

        for (int index = 0; index < ENERGY_VECTOR_COUNT; index++)
        {
            float4 vector = _energyVectors[index];

            if (vector.w <= 0f)
                continue;

            float length = math.length(vector.xyz);
            Vector3 tip = _listenerPosition + (Vector3)(vector.xyz * speakerRadius);

            Handles.color = colors[index];
            Handles.DrawLine(_listenerPosition, tip, index == ENERGY_VECTOR_COUNT - 1 ? 4f : 2f);
            Handles.Label(tip, $"{ENERGY_VECTOR_NAMES[index]}  {math.degrees(math.acos(math.saturate(length))):F1}°", style);
        }
    }

    void DrawLoudestSpeakers(GUIStyle style)
    {
        int count = math.min(labeledSpeakers, _loudestSpeakers.Length);

        for (int rank = 0; rank < count; rank++)
            _loudestSpeakers[rank] = -1;

        for (int speaker = 0; speaker < VirtualSpeakerLayout.COUNT; speaker++)
        {
            float magnitude = math.length(_speakerFeeds[speaker].xyz);

            for (int rank = 0; rank < count; rank++)
            {
                int held = _loudestSpeakers[rank];

                if (held >= 0 && math.length(_speakerFeeds[held].xyz) >= magnitude)
                    continue;

                for (int shift = count - 1; shift > rank; shift--)
                    _loudestSpeakers[shift] = _loudestSpeakers[shift - 1];

                _loudestSpeakers[rank] = speaker;
                break;
            }
        }

        for (int rank = 0; rank < count; rank++)
        {
            int speaker = _loudestSpeakers[rank];

            if (speaker < 0)
                break;

            float4 feed = _speakerFeeds[speaker];
            float relative = Decibels(math.length(feed.xyz) / _speakerPeak);
            bool negative = math.csum(feed.xyz) < 0f;

            Vector3 position = _listenerPosition + _listenerRotation * (Vector3)(VirtualSpeakerLayout.Directions[speaker] * (speakerRadius + 0.25f));
            Handles.Label(position, $"#{speaker} {relative:F1} dB{(negative ? " (-)" : "")}", style);
        }
    }

    static float Decibels(float amplitude) => 20f * math.log10(math.max(amplitude, 1e-9f));
}
#endif
