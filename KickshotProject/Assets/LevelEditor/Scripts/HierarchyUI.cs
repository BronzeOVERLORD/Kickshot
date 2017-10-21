using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HierarchyUI : MonoBehaviour {

    [HideInInspector]
    public DirectoryUI rootFolder;

    [Header ("References")]
    public GameObject outerPanel;
    public GameObject innerPanel;

    [Header ("Prefabs")]
    public GameObject folderPrefab;

    private Rect rect;

    private void Start()
    {
        string rootPath = Application.dataPath + "/LevelEditor/Structures";
        string[] directories = Directory.GetDirectories(rootPath);
        string[] files = Directory.GetFiles(rootPath, "*.prefab");

        GameObject folderObj = Instantiate(folderPrefab, Vector3.zero, Quaternion.identity);
        folderObj.transform.SetParent(innerPanel.transform);
        rootFolder = folderObj.GetComponent<DirectoryUI>();

        Debug.Log("Files");
        foreach (string file in files)
        {
            Debug.Log(file);
        }
        Debug.Log("Directories");
        foreach (string dir in directories)
        {
            Debug.Log(dir);
        }
    }
}
