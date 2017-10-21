using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HierarchyUI : MonoBehaviour {

    [HideInInspector]
    public DirectoryUI rootFolder;

    [Header ("References")]
    public RectTransform outerPanel;
    public RectTransform innerPanel;
    public string levelEditorAssetPath;

    [Header ("Prefabs")]
    public GameObject folderPrefab;

    private Rect rect;

    private void Start()
    {
        string rootPath = Application.dataPath + levelEditorAssetPath;

        //Instantiates all directories and assets
        GameObject folderObj = Instantiate(folderPrefab, innerPanel.transform.position, Quaternion.identity, innerPanel.gameObject.transform);
        rootFolder = folderObj.GetComponent<DirectoryUI>();
        RectTransform folderRect = folderObj.GetComponent<RectTransform>();
        rootFolder.folderPath = rootPath;
        string[] strs = rootPath.Split('/');
        rootFolder.FolderName = strs[strs.Length - 1];
        rootFolder.Open();

        Debug.Log("Total Height: " + rootFolder.GetHeight());
    }
}
