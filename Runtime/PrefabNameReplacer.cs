using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabNameReplacer : EditorWindow
{
    private string searchString = "Old";
    private string replaceString = "New";
    private string targetFolder = "Assets/Prefabs";

    [MenuItem("Tools/批量替换预制体名称")]
    public static void ShowWindow()
    {
        GetWindow<PrefabNameReplacer>("名称替换工具");
    }

    void OnGUI()
    {
        GUILayout.Label("名称替换配置", EditorStyles.boldLabel);
        searchString = EditorGUILayout.TextField("查找内容", searchString);
        replaceString = EditorGUILayout.TextField("替换内容", replaceString);
        targetFolder = EditorGUILayout.TextField("目标文件夹", targetFolder);

        if (GUILayout.Button("开始替换"))
        {
            ExecuteReplace();
        }
    }

    void ExecuteReplace()
    {
        List<string> prefabPaths = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolder });

        // 收集预制体路径
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            prefabPaths.Add(path);
        }

        int total = prefabPaths.Count;
        for (int i = 0; i < total; i++)
        {
            // 进度条显示
            EditorUtility.DisplayProgressBar("处理中", $"正在处理预制体 ({i+1}/{total})", (float)i / total);

            string path = prefabPaths[i];
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            bool modified = false;
            Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Contains(searchString))
                {
                    Undo.RecordObject(child.gameObject, "Rename Prefab Child");
                    child.name = child.name.Replace(searchString, replaceString);
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                AssetDatabase.SaveAssets();
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        Debug.Log($"处理完成，共修改 {prefabPaths.Count} 个预制体");
    }
}