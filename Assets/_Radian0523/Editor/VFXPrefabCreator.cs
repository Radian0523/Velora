#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Velora.Weapon;

namespace Velora.Editor
{
    /// <summary>
    /// Bloom 対応の光る VFX プレハブとマテリアルを自動生成するエディタツール。
    /// メニュー「Velora/Create VFX Prefabs」から実行する。
    /// URP Particles/Unlit + Additive + HDR カラーで Bloom に反応する発光エフェクトを生成。
    /// </summary>
    public static class VFXPrefabCreator
    {
        private const string PrefabFolder = "Assets/_Radian0523/Prefabs/VFX";
        private const string MaterialFolder = "Assets/_Radian0523/Art/Materials/VFX";

        [MenuItem("Velora/Create VFX Prefabs")]
        public static void Execute()
        {
            EnsureDirectory(PrefabFolder);
            EnsureDirectory(MaterialFolder);

            var additiveMat = CreateAdditiveMaterial();

            CreateMuzzleFlashPrefab(additiveMat);
            CreateImpactSparkPrefab(additiveMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VFXPrefabCreator] Glowing VFX prefabs created.");
        }

        // --- マテリアル ---

        private static Material CreateAdditiveMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError("[VFXPrefabCreator] URP Particles/Unlit shader not found.");
                return null;
            }

            var mat = new Material(shader);
            mat.name = "M_Particle_Additive";

            // Surface: Transparent, Blend: Additive
            mat.SetFloat("_Surface", 1f);       // Transparent
            mat.SetFloat("_BlendOp", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);

            // HDR ベースカラー: intensity > 1 で Bloom に反応する
            mat.SetColor("_BaseColor", new Color(3f, 3f, 3f, 1f));

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHATEST_ON");

            string matPath = MaterialFolder + "/M_Particle_Additive.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            return AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        // --- マズルフラッシュ ---

        private static void CreateMuzzleFlashPrefab(Material mat)
        {
            var root = new GameObject("MuzzleFlash_Default");

            // 放射状スパーク（飛び散る火花）のみ
            SetupMuzzleSparks(root, mat);

            root.AddComponent<PooledEffect>();

            string path = PrefabFolder + "/MuzzleFlash_Default.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void SetupMuzzleSparks(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
            if (mat != null) renderer.material = mat;

            var main = ps.main;
            main.duration = 0.05f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            // HDR 黄白: 中心部が白く光り、外側がオレンジに減衰
            main.startColor = new Color(5f, 3f, 1f, 1f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.01f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // --- 着弾エフェクト ---

        private static void CreateImpactSparkPrefab(Material mat)
        {
            var root = new GameObject("ImpactSpark_Default");

            // Layer 1: 着弾フラッシュ（衝突点の閃光）
            SetupImpactFlash(root, mat);

            // Layer 2: 飛散する火花
            var sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            SetupImpactSparks(sparksGo, mat);

            root.AddComponent<PooledEffect>();

            string path = PrefabFolder + "/ImpactSpark_Default.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void SetupImpactFlash(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (mat != null) renderer.material = mat;

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            main.startColor = new Color(4f, 3f, 1.5f, 1f);
            main.maxParticles = 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void SetupImpactSparks(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.5f;
            if (mat != null) renderer.material = mat;

            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
            main.startColor = new Color(5f, 3f, 0.8f, 1f);
            main.maxParticles = 25;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 3f;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.03f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 0.7f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // --- ユーティリティ ---

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
