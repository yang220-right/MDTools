using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PrefabNameProcessor : EditorWindow
{
    private string removePattern = "_old";
    private string prefix = "";
    private string suffix = "";
    private bool useRegex;
    private bool recursive = true;
    private Vector2 scrollPos;

    [MenuItem("Assets/处理预制体名称", true)]
    static bool ValidateSelection() => Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

    [MenuItem("Assets/处理预制体名称", false, 30)]
    static void Init() => GetWindow<PrefabNameProcessor>("名称处理工具").Show();

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("处理规则配置", EditorStyles.boldLabel);
        removePattern = EditorGUILayout.TextField("删除内容", removePattern);
        useRegex = EditorGUILayout.Toggle("使用正则表达式", useRegex);
        prefix = EditorGUILayout.TextField("添加前缀", prefix);
        suffix = EditorGUILayout.TextField("添加后缀", suffix);
        recursive = EditorGUILayout.Toggle("包含子文件夹", recursive);

        GUILayout.Space(20);
        
        if (GUILayout.Button("开始处理", GUILayout.Height(40)))
        {
            ProcessSelectedFolder();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void ProcessSelectedFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        List<string> prefabPaths = new List<string>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(folderPath))
                prefabPaths.Add(path);
        }

        int total = prefabPaths.Count;
        for (int i = 0; i < total; i++)
        {
            string path = prefabPaths[i];
            EditorUtility.DisplayProgressBar("处理中", 
                $"Processing: {Path.GetFileName(path)} ({i+1}/{total})", 
                (float)i / total);

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            bool modified = false;

            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                string originalName = child.name;
                string newName = originalName;

                // 删除处理
                if (!string.IsNullOrEmpty(removePattern))
                {
                    newName = useRegex ? 
                        Regex.Replace(newName, removePattern, "") : 
                        newName.Replace(removePattern, "");
                }

                // 添加前后缀
                newName = $"{prefix}{newName}{suffix}";

                if (newName != originalName)
                {
                    Undo.RecordObject(child, "Modify Object Name");
                    child.name = newName;
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log($"已修改: {path}");
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        Debug.Log($"处理完成，共修改 {prefabPaths.Count} 个预制体");
    }
}