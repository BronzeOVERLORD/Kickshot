using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelEditor
{
    public class LevelObject : MonoBehaviour
    {

        public int prefabID;

        /// <summary>
        /// Computes the the LevelObjectStruct for serialization from the LevelObject.
        /// </summary>
        /// <returns>The LevelObjectStruct.</returns>
        public LevelObjectStruct ComputeStruct() {
            LevelObjectStruct ret = new LevelObjectStruct
            {
                name = gameObject.name,
                id = prefabID,
                position = gameObject.transform.position,
                rotation = gameObject.transform.rotation,
                scale = gameObject.transform.localScale
            };
            return ret;
        }
    }

    /// <summary>
    /// Level object used for JSON serialization of structural prefabs.
    /// </summary>
    [System.Serializable]
    public struct LevelObjectStruct
    {
        public string name;
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public LevelObjectStruct(int ID)
        {
            name = "";
            id = ID;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
        }
    }
}