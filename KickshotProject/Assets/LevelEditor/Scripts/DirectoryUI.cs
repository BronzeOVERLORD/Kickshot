using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class DirectoryUI : MonoBehaviour {

    public Rect rect;
    public List<DirectoryUI> folders;
    public List<AssetUI> assets;
    public string folderPath;
    public string FolderName {
        get {
            return this.folderName;
        }
        set {
            folderText.text = value;
            this.folderName = value;
        }
    }
    public bool open = false;

    private string folderName;

    public Text folderText;

    [Header ("Prefabs")]
    public GameObject folderUIPrefab;
    public GameObject assetUIPrefab;

    /// <summary>
    /// Opens this directory.
    /// </summary>
    public void Open() {
        open = true;
        FindSubFolders();
        rect.height = GetHeight();
        Debug.Log("Folder Height: " + rect.height);
    }

    /// <summary>
    /// Closes this directory and all sub directories.
    /// </summary>
    public void Close() {
        foreach (DirectoryUI dir in folders) {
            dir.Close();
        }
        open = false;
        rect.height = GetHeight();
    }

    /// <summary>
    /// Searches the Assets directory and creates all sub directories.
    /// </summary>
    public void FindSubFolders() {
        if (folderPath.Equals("")) return;

        string[] directories = Directory.GetDirectories(folderPath);
        string[] files = Directory.GetFiles(folderPath, "*.prefab");

        foreach (string dir in directories) {
            GameObject newDir = Instantiate(folderUIPrefab, Vector3.zero, Quaternion.identity, transform);
            DirectoryUI newDirUI = newDir.GetComponent<DirectoryUI>();
            newDirUI.folderPath = dir;
            string[] path = dir.Split('/');
            newDirUI.FolderName = path[path.Length - 1];
            folders.Add(newDirUI);
        }

        foreach (string file in files) {
            GameObject newAsset = Instantiate(assetUIPrefab, Vector3.zero, Quaternion.identity, transform);
            AssetUI newAssetUI = newAsset.GetComponent<AssetUI>();
            newAssetUI.assetPath = file;
            assets.Add(newAssetUI);
        }
    }

    /// <summary>
    /// Gets the height of this directory using open.
    /// </summary>
    /// <returns>The height.</returns>
    public int GetHeight() {
        int height = 30;
        if (open)
        {
            foreach (DirectoryUI dir in folders)
            {
                if (dir.open)
                {
                    height += dir.GetHeight();
                }
                else
                {
                    height += 30;
                }
            }
            foreach (AssetUI asset in assets)
            {
                height += 30;
            }
        }
        return height;
    }
}
