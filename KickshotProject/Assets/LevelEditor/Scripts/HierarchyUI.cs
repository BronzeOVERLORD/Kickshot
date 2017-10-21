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
    public string levelEditorAssetPath;

    [Header ("Prefabs")]
    public GameObject folderPrefab;

    private Rect rect;

    private void Start()
    {
        string rootPath = Application.dataPath + levelEditorAssetPath;

        GameObject folderObj = Instantiate(folderPrefab, Vector3.zero, Quaternion.identity, innerPanel.transform);
        rootFolder = folderObj.GetComponent<DirectoryUI>();
        rootFolder.folderPath = rootPath;
        string[] strs = rootPath.Split('/');
        rootFolder.FolderName = strs[strs.Length - 1];
        rootFolder.Open();

        Debug.Log("Total Height: " + rootFolder.GetHeight());
    }
}
