#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Velora.Editor
{
    /// <summary>
    /// ParticleSystem ベースの VFX Prefab から任意の色バリアントを生成するエディタウィンドウ。
    /// ソース Prefab 内の全 ParticleSystem の startColor / ColorOverLifetime を
    /// HSV 色相シフトで変換し、新しい Prefab として保存する。
    /// 彩度・明度・アルファは元の値を維持するため、エフェクトの見た目の質感はそのまま色だけ変わる。
    /// 用途: Magic shields 等のパーティクルエフェクトの色違い量産。
    /// </summary>
    public class VFXColorVariantCreator : EditorWindow
    {
        private const string OutputFolder = "Assets/_Radian0523/Prefabs/VFX";

        private static readonly string[] KnownColorSuffixes =
        {
            "blue", "pink", "yellow", "red", "green", "purple",
            "orange", "white", "cyan", "magenta"
        };

        [SerializeField] private GameObject _sourcePrefab;
        [SerializeField] private Color _targetColor = Color.green;
        [SerializeField] private string _variantName = "green";

        private Vector2 _scrollPosition;

        [MenuItem("Velora/VFX/Create Color Variant")]
        private static void ShowWindow()
        {
            var window = GetWindow<VFXColorVariantCreator>("VFX Color Variant");
            window.minSize = new Vector2(350, 220);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("VFX Color Variant Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Source Prefab", _sourcePrefab, typeof(GameObject), false);
            _targetColor = EditorGUILayout.ColorField(
                new GUIContent("Target Color"), _targetColor, true, false, false);
            _variantName = EditorGUILayout.TextField("Variant Name", _variantName);

            EditorGUILayout.Space();

            bool canCreate = _sourcePrefab != null && !string.IsNullOrWhiteSpace(_variantName);
            EditorGUI.BeginDisabledGroup(!canCreate);
            if (GUILayout.Button("Create Variant", GUILayout.Height(30)))
            {
                CreateVariant();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "ソース Prefab 内の全 ParticleSystem の色相を Target Color に合わせた\n" +
                "新しい Prefab を生成します。彩度・明度・アルファは元の値を維持します。",
                MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void CreateVariant()
        {
            string sourcePath = AssetDatabase.GetAssetPath(_sourcePrefab);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("[VFXColorVariantCreator] ソース Prefab のパスが取得できません。");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(sourcePath);
            var particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

            if (particleSystems.Length == 0)
            {
                Debug.LogWarning("[VFXColorVariantCreator] ParticleSystem が見つかりません。");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            float sourceHue = DetectSourceHue(particleSystems);
            Color.RGBToHSV(_targetColor, out float targetHue, out _, out _);
            float hueShift = targetHue - sourceHue;

            foreach (var ps in particleSystems)
            {
                ShiftStartColor(ps, hueShift);
                ShiftColorOverLifetime(ps, hueShift);
            }

            string baseName = RemoveColorSuffix(_sourcePrefab.name);
            string newName = $"{baseName} {_variantName}";
            root.name = newName;

            EnsureDirectory(OutputFolder);
            string outputPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{OutputFolder}/{newName}.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VFXColorVariantCreator] Created: {outputPath}");

            var created = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            EditorGUIUtility.PingObject(created);
        }

        // --- 色相シフト ---

        /// <summary>
        /// ソース内で最も彩度の高い startColor の色相をエフェクトの「代表色」として返す。
        /// </summary>
        private static float DetectSourceHue(ParticleSystem[] systems)
        {
            float bestSaturation = 0f;
            float bestHue = 0f;

            foreach (var ps in systems)
            {
                var startColor = ps.main.startColor;
                UpdateBestHue(startColor.color, ref bestHue, ref bestSaturation);

                if (startColor.mode == ParticleSystemGradientMode.TwoColors)
                {
                    UpdateBestHue(startColor.colorMin, ref bestHue, ref bestSaturation);
                    UpdateBestHue(startColor.colorMax, ref bestHue, ref bestSaturation);
                }
            }

            return bestHue;
        }

        private static void UpdateBestHue(Color color, ref float bestHue, ref float bestSaturation)
        {
            Color.RGBToHSV(color, out float h, out float s, out _);
            if (s > bestSaturation)
            {
                bestSaturation = s;
                bestHue = h;
            }
        }

        private static void ShiftStartColor(ParticleSystem ps, float hueShift)
        {
            var main = ps.main;
            var startColor = main.startColor;

            switch (startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    startColor.color = ShiftHue(startColor.color, hueShift);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    startColor.colorMin = ShiftHue(startColor.colorMin, hueShift);
                    startColor.colorMax = ShiftHue(startColor.colorMax, hueShift);
                    break;
                case ParticleSystemGradientMode.Gradient:
                    startColor.gradient = ShiftGradientHue(startColor.gradient, hueShift);
                    break;
                case ParticleSystemGradientMode.TwoGradients:
                    startColor.gradientMin = ShiftGradientHue(startColor.gradientMin, hueShift);
                    startColor.gradientMax = ShiftGradientHue(startColor.gradientMax, hueShift);
                    break;
            }

            main.startColor = startColor;
        }

        /// <summary>
        /// ColorOverLifetime のグラデーション内に色情報がある場合もシフトする。
        /// アルファのみのグラデーション（色キーが白）でも安全に処理できる。
        /// </summary>
        private static void ShiftColorOverLifetime(ParticleSystem ps, float hueShift)
        {
            var colorModule = ps.colorOverLifetime;
            if (!colorModule.enabled) return;

            var color = colorModule.color;

            switch (color.mode)
            {
                case ParticleSystemGradientMode.Color:
                    color.color = ShiftHue(color.color, hueShift);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    color.colorMin = ShiftHue(color.colorMin, hueShift);
                    color.colorMax = ShiftHue(color.colorMax, hueShift);
                    break;
                case ParticleSystemGradientMode.Gradient:
                    color.gradient = ShiftGradientHue(color.gradient, hueShift);
                    break;
                case ParticleSystemGradientMode.TwoGradients:
                    color.gradientMin = ShiftGradientHue(color.gradientMin, hueShift);
                    color.gradientMax = ShiftGradientHue(color.gradientMax, hueShift);
                    break;
            }

            colorModule.color = color;
        }

        private static Color ShiftHue(Color source, float hueShift)
        {
            Color.RGBToHSV(source, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueShift, 1f);
            Color result = Color.HSVToRGB(h, s, v);
            result.a = source.a;
            return result;
        }

        private static Gradient ShiftGradientHue(Gradient source, float hueShift)
        {
            if (source == null) return null;

            var colorKeys = source.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = ShiftHue(colorKeys[i].color, hueShift);
            }

            var result = new Gradient();
            result.SetKeys(colorKeys, source.alphaKeys);
            result.mode = source.mode;
            return result;
        }

        // --- ユーティリティ ---

        /// <summary>
        /// Prefab 名末尾の既知の色名サフィックスを除去する。
        /// "Magic shield blue" → "Magic shield"
        /// </summary>
        private static string RemoveColorSuffix(string name)
        {
            string lower = name.ToLower();
            foreach (string suffix in KnownColorSuffixes)
            {
                if (lower.EndsWith(suffix))
                {
                    return name.Substring(0, name.Length - suffix.Length).TrimEnd();
                }
            }
            return name;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
