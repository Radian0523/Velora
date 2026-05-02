using System.IO;
using UnityEditor;
using UnityEngine;
using Velora.Data;

namespace Velora.Editor
{
    /// <summary>
    /// プレハブを一時的にインスタンス化し、横からカメラで撮影して
    /// 透過 PNG のアイコンを生成するエディタツール。
    /// </summary>
    public static class WeaponIconCapture
    {
        private const int IconSize = 512;

        [MenuItem("Velora/Capture Weapon Icons")]
        public static void CaptureAll()
        {
            string outputDir = "Assets/_Radian0523/Art/UI/Icons";
            EnsureFolderExists(outputDir);

            CaptureWeaponIcon(
                "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Sci Fi Pistol.prefab",
                $"{outputDir}/Weapon_Pistol.png",
                "Assets/_Radian0523/ScriptableObjects/Weapon/Pistol.asset");

            CaptureWeaponIcon(
                "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Railgun 1.prefab",
                $"{outputDir}/Weapon_Railgun1.png",
                null);

            CaptureWeaponIcon(
                "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Railgun 2.prefab",
                $"{outputDir}/Weapon_Railgun2.png",
                null);

            CaptureWeaponIcon(
                "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Sci Fi SMG.prefab",
                $"{outputDir}/Weapon_SMG.png",
                null);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[WeaponIconCapture] All icons captured.");
        }

        private static void CaptureWeaponIcon(string prefabPath, string outputPath, string weaponSOPath)
        {
            // プレハブをロード＆インスタンス化
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[WeaponIconCapture] Prefab not found: {prefabPath}");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            // Bounds を計算してカメラ位置を決める
            var bounds = CalculateBounds(instance);
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            // カメラを右側（+X 方向）から左を向くように配置
            var cameraObj = new GameObject("IconCamera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.z) * 1.2f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = maxExtent * 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.cullingMask = ~0;

            // 右から撮影（+X → -X 方向を向く）
            cameraObj.transform.position = bounds.center + Vector3.right * (maxExtent * 3f);
            cameraObj.transform.LookAt(bounds.center);

            // ライトを用意
            var lightObj = new GameObject("IconLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(30f, -45f, 0f);

            // RenderTexture にレンダリング
            var rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            camera.targetTexture = rt;
            camera.Render();

            // RenderTexture → Texture2D → PNG
            RenderTexture.active = rt;
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, png);
            Debug.Log($"[WeaponIconCapture] Saved: {outputPath}");

            // 後片付け
            camera.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cameraObj);
            Object.DestroyImmediate(lightObj);
            Object.DestroyImmediate(instance);

            // テクスチャインポート設定を Sprite に変更
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            // WeaponData に Sprite をアサイン
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
            if (sprite == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(outputPath))
                {
                    if (obj is Sprite s) { sprite = s; break; }
                }
            }

            if (sprite != null && !string.IsNullOrEmpty(weaponSOPath))
            {
                var weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponSOPath);
                if (weaponData != null)
                {
                    var so = new SerializedObject(weaponData);
                    so.FindProperty("_icon").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(weaponData);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[WeaponIconCapture] Assigned to {weaponData.WeaponName}");
                }
            }
        }

        private static Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
