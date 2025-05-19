using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class FolderContextMenuReplacer
{
    private const string MenuPath = "Assets/批量替换文件夹内预制体名称";

    [MenuItem(MenuPath, true)]
    private static bool ValidateSelection()
    {
        // 验证选中的是文件夹
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.IsValidFolder(path);
        }
        return false;
    }

    [MenuItem(MenuPath, false, 31)]
    private static void InitReplace()
    {
        var window = EditorWindow.GetWindow<FolderReplaceConfigWindow>("文件夹替换配置");
        window.minSize = new Vector2(350, 150);
    }

    // 配置窗口
    public class FolderReplaceConfigWindow : EditorWindow
    {
        private string searchString = "Old";
        private string replaceString = "New";
        private bool includeSubfolders = true;

        void OnGUI()
        {
            GUILayout.Label("文件夹批量替换配置", EditorStyles.boldLabel);
            searchString = EditorGUILayout.TextField("查找内容", searchString);
            replaceString = EditorGUILayout.TextField("替换内容", replaceString);
            includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);

            GUILayout.Space(20);

            if (GUILayout.Button("开始替换", GUILayout.Height(30)))
            {
                ExecuteFolderReplace();
                Close();
            }
        }

        void ExecuteFolderReplace()
        {
            int totalModified = 0;
            int totalPrefabs = 0;

            foreach (var obj in Selection.objects)
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                if (!AssetDatabase.IsValidFolder(folderPath)) continue;

                // 获取所有预制体路径
                var searchPattern = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                totalPrefabs += guids.Length;

                // 处理每个预制体
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("处理中", 
                        $"正在处理: {Path.GetFileName(path)} ({i+1}/{guids.Length})", 
                        (float)i / guids.Length);

                    if (ProcessPrefab(path)) 
                        totalModified++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成",
                $"扫描预制体: {totalPrefabs} 个\n成功修改: {totalModified} 个", 
                "确定");
        }

        bool ProcessPrefab(string path)
        {
            bool modified = false;
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.Contains(searchString)) 
                    continue;

                Undo.RegisterCompleteObjectUndo(child, "Rename Prefab Child");
                child.name = child.name.Replace(searchString, replaceString);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
                return true;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
            return false;
        }
    }
}