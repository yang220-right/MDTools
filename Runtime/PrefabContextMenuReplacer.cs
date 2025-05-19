using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabContextMenuReplacer
{
    private const string MenuPath = "Assets/替换Prefab内的对象name";

    [MenuItem(MenuPath, true)]
    private static bool ValidateSelection()
    {
        // 仅当选中预制体时显示菜单项
        foreach (var obj in Selection.objects)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
                return true;
        }
        return false;
    }

    [MenuItem(MenuPath, false, 30)]
    private static void InitReplace()
    {
        // 创建替换配置窗口
        var window = EditorWindow.GetWindow<ReplaceConfigWindow>("替换配置");
        window.minSize = new Vector2(300, 120);
    }

    // 替换配置窗口
    public class ReplaceConfigWindow : EditorWindow
    {
        private string searchString = "Old";
        private string replaceString = "New";

        void OnGUI()
        {
            GUILayout.Label("替换参数配置", EditorStyles.boldLabel);
            searchString = EditorGUILayout.TextField("查找字符串", searchString);
            replaceString = EditorGUILayout.TextField("替换字符串", replaceString);

            GUILayout.Space(20);

            if (GUILayout.Button("执行替换"))
            {
                ExecuteReplace();
                Close();
            }
        }

        void ExecuteReplace()
        {
            int modifiedCount = 0;
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!PrefabUtility.IsPartOfPrefabAsset(obj)) continue;

                // 加载预制体内容
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                bool isModified = false;

                // 遍历所有子物体
                Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name.Contains(searchString))
                    {
                        Undo.RecordObject(child, "Rename Prefab Child");
                        child.name = child.name.Replace(searchString, replaceString);
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    // 保存修改
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    modifiedCount++;
                }
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", 
                $"已处理 {Selection.objects.Length} 个预制体\n成功修改 {modifiedCount} 个", "确定");
        }
    }
}