#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CompositionAnalyzer))]
public class CompositionAnalyzerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        CompositionAnalyzer analyzer =
            (CompositionAnalyzer)target;

        if (GUILayout.Button("ANALYZE ARTWORK"))
        {
            analyzer.DebugAnalyze();
        }
    }
}

#endif