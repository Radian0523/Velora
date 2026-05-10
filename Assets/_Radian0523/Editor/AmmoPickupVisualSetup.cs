using UnityEditor;
using UnityEngine;

/// <summary>
/// AmmoPickup プレハブの視覚的差別化を一括セットアップするエディタスクリプト。
/// 弾薬タイプごとに異なるメッシュ・マテリアル・スケールを割り当てることで、
/// プレイヤーがピックアップの種類を一目で判別できるようにする。
/// </summary>
public static class AmmoPickupVisualSetup
{
    private struct AmmoVisualConfig
    {
        public string PrefabPath;
        public PrimitiveType MeshPrimitive;
        public Vector3 ModelScale;
        public Vector3 ModelRotation;
        public Color BaseColor;
        public Color EmissionColor;
        public string MaterialName;
    }

    [MenuItem("Velora/Setup Ammo Pickup Visuals")]
    public static void Execute()
    {
        CreateMaterials();
        UpdatePrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AmmoPickupVisualSetup] Complete.");
    }

    private static void CreateMaterials()
    {
        const string folder = "Assets/_Radian0523/Materials/Pickup";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/_Radian0523/Materials", "Pickup");
        }

        CreateEmissiveMaterial(
            $"{folder}/M_Pickup_Light.mat",
            new Color(1f, 0.843f, 0f),
            new Color(2f, 1.686f, 0f));

        CreateEmissiveMaterial(
            $"{folder}/M_Pickup_Energy.mat",
            new Color(0f, 0.898f, 1f),
            new Color(0f, 1.796f, 2f));

        CreateEmissiveMaterial(
            $"{folder}/M_Pickup_Explosive.mat",
            new Color(1f, 0.188f, 0.188f),
            new Color(2f, 0.376f, 0.376f));

        Debug.Log("[AmmoPickupVisualSetup] Materials created.");
    }

    private static void CreateEmissiveMaterial(string path, Color baseColor, Color emissionColor)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[AmmoPickupVisualSetup] URP Lit shader not found.");
            return;
        }

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.7f);

        // Emission を有効化して HDR カラーを設定。URP Bloom と連動してグロー効果を得る。
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        AssetDatabase.CreateAsset(mat, path);
        Debug.Log($"[AmmoPickupVisualSetup] Created: {path}");
    }

    private static void UpdatePrefabs()
    {
        var configs = new AmmoVisualConfig[]
        {
            // Light: 弾丸を連想させる縦長カプセル
            new AmmoVisualConfig
            {
                PrefabPath = "Assets/_Radian0523/Prefabs/Weapon/Ammo Pickup/AmmoPickup_Light.prefab",
                MeshPrimitive = PrimitiveType.Capsule,
                ModelScale = new Vector3(0.4f, 0.6f, 0.4f),
                ModelRotation = Vector3.zero,
                MaterialName = "M_Pickup_Light"
            },
            // Energy: クリスタル風ダイヤモンド形状（45度回転した Cube）
            new AmmoVisualConfig
            {
                PrefabPath = "Assets/_Radian0523/Prefabs/Weapon/Ammo Pickup/AmmoPickup_Energy.prefab",
                MeshPrimitive = PrimitiveType.Cube,
                ModelScale = new Vector3(0.5f, 0.5f, 0.5f),
                ModelRotation = new Vector3(45f, 0f, 45f),
                MaterialName = "M_Pickup_Energy"
            },
            // Explosive: 手榴弾を連想させる球体
            new AmmoVisualConfig
            {
                PrefabPath = "Assets/_Radian0523/Prefabs/Weapon/Ammo Pickup/AmmoPickup_Explosive.prefab",
                MeshPrimitive = PrimitiveType.Sphere,
                ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                ModelRotation = Vector3.zero,
                MaterialName = "M_Pickup_Explosive"
            },
            // ベースプレハブも Light と同じ見た目に更新
            new AmmoVisualConfig
            {
                PrefabPath = "Assets/_Radian0523/Prefabs/Weapon/AmmoPickup.prefab",
                MeshPrimitive = PrimitiveType.Capsule,
                ModelScale = new Vector3(0.4f, 0.6f, 0.4f),
                ModelRotation = Vector3.zero,
                MaterialName = "M_Pickup_Light"
            }
        };

        foreach (var config in configs)
        {
            UpdatePrefabModel(config);
        }
    }

    private static void UpdatePrefabModel(AmmoVisualConfig config)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[AmmoPickupVisualSetup] Prefab not found: {config.PrefabPath}");
            return;
        }

        var matPath = $"Assets/_Radian0523/Materials/Pickup/{config.MaterialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            Debug.LogError($"[AmmoPickupVisualSetup] Material not found: {matPath}");
            return;
        }

        // Prefab の内容を直接編集する
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        // "Model" という名前の子オブジェクトを探す。なければ最初の子を使う。
        Transform modelTransform = root.transform.Find("Model");
        if (modelTransform == null && root.transform.childCount > 0)
        {
            modelTransform = root.transform.GetChild(0);
        }

        if (modelTransform == null)
        {
            Debug.LogWarning($"[AmmoPickupVisualSetup] No child object found in {config.PrefabPath}");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // MeshFilter のメッシュを差し替え
        var meshFilter = modelTransform.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = modelTransform.gameObject.AddComponent<MeshFilter>();
        }

        var tempPrimitive = GameObject.CreatePrimitive(config.MeshPrimitive);
        meshFilter.sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tempPrimitive);

        // MeshRenderer のマテリアルを差し替え
        var meshRenderer = modelTransform.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = modelTransform.gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.sharedMaterial = material;

        // スケール・回転を設定
        modelTransform.localScale = config.ModelScale;
        modelTransform.localEulerAngles = config.ModelRotation;

        // プリミティブ生成で追加される Collider を除去（トリガー判定は親の BoxCollider が担当）
        var collider = modelTransform.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[AmmoPickupVisualSetup] Updated: {config.PrefabPath}");
    }
}
