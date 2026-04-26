#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.Editor
{
    /// <summary>
    /// HUD Canvas + クロスヘア Image をシーンに配置するセットアップツール。
    /// メニュー「Velora/Setup Crosshair」から実行。
    /// </summary>
    public static class CrosshairSetup
    {
        private const string TexturePath =
            "Assets/_External/OccaSoftware/Crosshairs/Art/Textures/Crosshair_04.png";

        [MenuItem("Velora/Setup Crosshair")]
        public static void Execute()
        {
            EnsureSpriteImport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            if (sprite == null)
            {
                Debug.LogError("[CrosshairSetup] Sprite not found: " + TexturePath);
                return;
            }

            // Canvas
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Crosshair Image
            var crosshairGo = new GameObject("Crosshair");
            crosshairGo.transform.SetParent(canvasGo.transform, false);

            var rect = crosshairGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(32f, 32f);

            var image = crosshairGo.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, 0.8f);
            image.raycastTarget = false;

            crosshairGo.AddComponent<UI.CrosshairView>();

            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[CrosshairSetup] HUD Canvas + Crosshair created.");
        }

        private static void EnsureSpriteImport()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }
}
#endif
