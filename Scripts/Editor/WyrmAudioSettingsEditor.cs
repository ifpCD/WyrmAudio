using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WyrmAudioSettings))]
public partial class WyrmAudioSettingsEditor : Editor
{
    private class NamespaceNode
    {
        public string Name;
        public Dictionary<string, NamespaceNode> SubNodes = new Dictionary<string, NamespaceNode>();
        public List<(string fieldName, int index)> Banks = new List<(string fieldName, int index)>();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(20);
        
        if (GUILayout.Button("Generate C# Wyrm Mixer Class", GUILayout.Height(35)))
        {
            GenerateStaticClass();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Generate C# Sound Banks Class", GUILayout.Height(35)))
        {
            GenerateSoundBanksClass();
        }

        GUI.backgroundColor = Color.white;
    }
}