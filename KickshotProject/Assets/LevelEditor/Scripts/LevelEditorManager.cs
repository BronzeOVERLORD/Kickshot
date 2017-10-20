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
            string json = JsonifyObjects(lvlObjs);
            Debug.Log(json);

            Debug.Log((Application.persistentDataPath)); 
            FileStream file = new FileStream(Application.persistentDataPath + "/level.json", FileMode.OpenOrCreate);
            file.Write(Encoding.UTF8.GetBytes(json), 0, json.Length);
        }

        private static string JsonifyObjects(List<LevelObject> objs)
        {
            string ret = "";
            for (int i = 0; i < objs.Count; i++) {
                ret += "\n" + objs[i].ToJson();
            }
            return ret;
        }
    }



}