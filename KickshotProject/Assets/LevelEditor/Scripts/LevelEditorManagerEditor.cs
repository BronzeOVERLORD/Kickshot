using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LevelEditor
{
    [CustomEditor(typeof(LevelEditorManager))]
    public class LevelEditorManagerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.TextField(LevelEditorManager.objectTag);

            if (GUILayout.Button("Export Level"))
            {
                LevelEditorManager.Export();
            }
        }
    }
}