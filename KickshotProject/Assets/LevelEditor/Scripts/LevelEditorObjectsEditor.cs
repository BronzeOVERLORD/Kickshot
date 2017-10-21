using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LevelEditor
{
    [CustomEditor(typeof(LevelEditorObjects))]
    public class LevelEditorObjectsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Update LevelEditorObjects"))
            {
                LevelEditorObjects.Reload();
            }
        }
    }
}