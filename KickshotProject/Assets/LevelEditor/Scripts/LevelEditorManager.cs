using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace LevelEditor
{
    public class LevelEditorManager : Singleton<LevelEditorManager>
    {
        public static string objectTag = "LevelObject";

        /// <summary>
        /// Exports the level to json
        /// </summary>
        public static void Export()
        {
            List<GameObject> objs = new List<GameObject>(GameObject.FindGameObjectsWithTag(objectTag));
            List<LevelObject> lvlObjs = new List<LevelObject>();
            foreach (GameObject o in objs) {
                LevelObject lo = o.GetComponent<LevelObject>();
                if (lo != null) {
                    lvlObjs.Add(lo);
                }
            }
            string json = JsonObjects(lvlObjs);
            FileStream file = new FileStream(Application.persistentDataPath + "/level.json", FileMode.OpenOrCreate);
            file.Write(Encoding.UTF8.GetBytes(json), 0, json.Length);
        }

        /// <summary>
        /// Imports a level from json
        /// </summary>
        public static void Import(string filePath)
        {
            
        }

        private static string JsonObjects(List<LevelObject> objs)
        {
            string ret = "[";
            for (int i = 0; i < objs.Count; i++)
            {

                LevelObjectStruct lvlObjS = objs[i].ComputeStruct();
                if (i < objs.Count - 1)
                {
                    ret += JsonUtility.ToJson(lvlObjS) + ",";
                }
                else
                {
                    ret += JsonUtility.ToJson(lvlObjS) + "]";
                }
            }
            return ret;
        }
    }
}