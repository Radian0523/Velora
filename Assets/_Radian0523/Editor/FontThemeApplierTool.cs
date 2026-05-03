using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Velora.UI;

namespace Velora.Editor
{
    /// <summary>
    /// シーン・プレファブ内の全 TMP_Text に FontThemeApplier を一括追加するエディタ拡張。
    /// 新しい UI を追加した後や、付与漏れの確認に使用する。
    /// Undo 対応のため、誤操作時は Ctrl+Z で元に戻せる。
    /// </summary>
    public static class FontThemeApplierTool
    {
        private const string ProjectRoot = "Assets/_Radian0523";

        [MenuItem("Velora/Font Theme/Add Applier to Open Scenes", priority = 200)]
        private static void AddToOpenScenes()
        {
            var texts = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int added = 0;
            foreach (var text in texts)
            {
                if (text.GetComponent<FontThemeApplier>() != null) continue;

                Undo.AddComponent<FontThemeApplier>(text.gameObject);
                added++;
            }

            if (added > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
            }

            EditorUtility.DisplayDialog(
                "Font Theme Applier",
                $"TMP_Text {texts.Length} 個中、{added} 個に FontThemeApplier を追加しました。",
                "OK");
        }

        [MenuItem("Velora/Font Theme/Add Applier to All Scenes", priority = 201)]
        private static void AddToAllScenes()
        {
            var currentSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { ProjectRoot });

            int totalAdded = 0;
            int sceneCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                sceneCount++;

                var texts = Object.FindObjectsByType<TMP_Text>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                int added = 0;
                foreach (var text in texts)
                {
                    if (text.GetComponent<FontThemeApplier>() != null) continue;

                    text.gameObject.AddComponent<FontThemeApplier>();
                    added++;
                }

                if (added > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    totalAdded += added;
                }
            }

            // 元のシーン構成を復元
            if (currentSceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(currentSceneSetup);
            }

            EditorUtility.DisplayDialog(
                "Font Theme Applier",
                $"シーン {sceneCount} 個を走査し、{totalAdded} 個の TMP_Text に FontThemeApplier を追加しました。",
                "OK");
        }

        [MenuItem("Velora/Font Theme/Add Applier to All Prefabs", priority = 202)]
        private static void AddToPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectRoot });
            int scanned = 0;
            int added = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab.GetComponentInChildren<TMP_Text>(true) == null) continue;
                scanned++;

                var contents = PrefabUtility.LoadPrefabContents(path);
                bool modified = false;

                foreach (var text in contents.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponent<FontThemeApplier>() != null) continue;

                    text.gameObject.AddComponent<FontThemeApplier>();
                    modified = true;
                    added++;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }

            EditorUtility.DisplayDialog(
                "Font Theme Applier",
                $"プレファブ {scanned} 個を走査し、{added} 個の TMP_Text に FontThemeApplier を追加しました。",
                "OK");
        }

        [MenuItem("Velora/Font Theme/Check Missing Appliers", priority = 210)]
        private static void CheckMissing()
        {
            var texts = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int missing = 0;
            foreach (var text in texts)
            {
                if (text.GetComponent<FontThemeApplier>() != null) continue;

                Debug.LogWarning(
                    $"FontThemeApplier 未設定: {GetHierarchyPath(text.gameObject)}",
                    text.gameObject);
                missing++;
            }

            if (missing == 0)
            {
                Debug.Log("全ての TMP_Text に FontThemeApplier が設定されています。");
            }
            else
            {
                Debug.LogWarning($"FontThemeApplier 未設定の TMP_Text が {missing} 個あります。");
            }
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
