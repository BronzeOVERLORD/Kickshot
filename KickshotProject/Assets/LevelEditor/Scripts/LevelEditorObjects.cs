using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LevelEditor
{
    /// <summary>
    /// Class containing the prefab database and helper methods.
    /// </summary>
    public class LevelEditorObjects : Singleton<LevelEditorObjects>
    {

        private static Dictionary<int, string> prefabs = new Dictionary<int, string>();

        /// <summary>
        /// Reload the Level Editor objects dictionary.
        /// </summary>
        public static void Reload() 
        {
            string rootPath = Application.dataPath + "/LevelEditor/Structures";
            string[] directories = Directory.GetDirectories(rootPath);
            string[] files = Directory.GetFiles(rootPath, "*.prefab");
            Debug.Log("Files");
            foreach (string file in files) {
                Debug.Log(file);
            }
            Debug.Log("Directories");
            foreach (string dir in directories) {
                Debug.Log(dir);
            }
        }

        /// <summary>
        /// Gets the prefab identifier from a GameObject.
        /// </summary>
        /// <returns>The prefab identifier.</returns>
        /// <param name="obj">Object.</param>
        public static int GetPrefabID(GameObject obj) 
        {

            return 0;
        }

        /// <summary>
        /// Gets the prefab path from the local prefab dictionary.
        /// </summary>
        /// <returns>The prefab path.</returns>
        /// <param name="prefabID">Prefab identifier.</param>
        public static string GetPrefabPath(int prefabID)
        {
            return "";
        }

        /// <summary>
        /// Gets the prefab identifier from a path to prefab.
        /// </summary>
        /// <returns>The prefab identifier.</returns>
        /// <param name="prefabPath">Prefab path.</param>
        public static int GetPrefabID(string prefabPath)
        {
            return 0;
        }

        /// <summary>
        /// Gets a prefab game object from its ID.
        /// </summary>
        /// <returns>The prefab game object.</returns>
        /// <param name="prefabID">Prefab identifier.</param>
        public static GameObject GetPrefabGameObject(int prefabID)
        {
            return null;
        }

        /// <summary>
        /// Gets the prefab game object from its path.
        /// </summary>
        /// <returns>The prefab game object.</returns>
        /// <param name="prefabPath">Prefab path.</param>
        public static GameObject GetPrefabGameObject(string prefabPath)
        {
            return null;
        }
    }
}