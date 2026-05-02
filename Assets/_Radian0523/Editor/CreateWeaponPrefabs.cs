using UnityEditor;
using UnityEngine;

/// <summary>
/// 武器モデルのラッパープレハブを一括生成するエディタスクリプト。
/// _External の元プレハブを変更せず、WeaponModelView + MuzzlePoint を持つ
/// ラッパープレハブを Prefabs/Weapon/ に作成する。
/// </summary>
public static class CreateWeaponPrefabs
{
    private struct WeaponPrefabConfig
    {
        public string SourcePrefabPath;
        public string OutputPrefabName;
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        public Vector3 MuzzleLocalPosition;
    }

    [MenuItem("Velora/Create Weapon Prefabs")]
    public static void Execute()
    {
        var configs = new WeaponPrefabConfig[]
        {
            new WeaponPrefabConfig
            {
                SourcePrefabPath = "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Sci Fi Pistol.prefab",
                OutputPrefabName = "Weapon_Pistol",
                LocalPosition = new Vector3(-0.006f, -0.002f, -0.193f),
                LocalRotation = new Vector3(0f, 357.90686f, 0f),
                MuzzleLocalPosition = new Vector3(-0.025f, 0.185f, 0.462f)
            },
            new WeaponPrefabConfig
            {
                SourcePrefabPath = "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Sci Fi SMG.prefab",
                OutputPrefabName = "Weapon_SMG",
                LocalPosition = new Vector3(-0.01f, -0.1f, -0.16f),
                LocalRotation = Vector3.zero,
                MuzzleLocalPosition = new Vector3(-0.015f, 0.283f, 0.429f)
            },
            new WeaponPrefabConfig
            {
                SourcePrefabPath = "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Railgun 1.prefab",
                OutputPrefabName = "Weapon_Railgun1",
                LocalPosition = new Vector3(-0.01f, -0.1f, -0.16f),
                LocalRotation = Vector3.zero,
                MuzzleLocalPosition = new Vector3(-0.015f, 0.283f, 0.429f)
            },
            new WeaponPrefabConfig
            {
                SourcePrefabPath = "Assets/_External/The Developer Train/Sci Fi Guns/Prefabs/Railgun 2.prefab",
                OutputPrefabName = "Weapon_Railgun2",
                LocalPosition = new Vector3(-0.01f, -0.1f, -0.16f),
                LocalRotation = Vector3.zero,
                MuzzleLocalPosition = new Vector3(-0.015f, 0.283f, 0.429f)
            }
        };

        string outputFolder = "Assets/_Radian0523/Prefabs/Weapon";

        foreach (var config in configs)
        {
            CreateWrapperPrefab(config, outputFolder);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateWeaponPrefabs] All weapon prefabs created.");
    }

    private static void CreateWrapperPrefab(WeaponPrefabConfig config, string outputFolder)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError($"[CreateWeaponPrefabs] Source prefab not found: {config.SourcePrefabPath}");
            return;
        }

        // ラッパー用の空 GameObject を作成
        var wrapper = new GameObject(config.OutputPrefabName);

        // WeaponModelView コンポーネントを追加
        var modelView = wrapper.AddComponent<Velora.Weapon.WeaponModelView>();

        // ソースプレハブをインスタンス化して子に配置
        var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, wrapper.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.Euler(config.LocalRotation);
        modelInstance.transform.localScale = Vector3.one;

        // MuzzlePoint を作成して銃口位置に配置
        var muzzlePoint = new GameObject("MuzzlePoint");
        muzzlePoint.transform.SetParent(wrapper.transform, false);
        muzzlePoint.transform.localPosition = config.MuzzleLocalPosition;

        // WeaponModelView の _muzzlePoint にシリアライズフィールドを設定
        var so = new SerializedObject(modelView);
        var muzzleProp = so.FindProperty("_muzzlePoint");
        muzzleProp.objectReferenceValue = muzzlePoint.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // プレハブとして保存
        string prefabPath = $"{outputFolder}/{config.OutputPrefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);

        // シーン上の一時オブジェクトを削除
        Object.DestroyImmediate(wrapper);

        Debug.Log($"[CreateWeaponPrefabs] Created: {prefabPath}");
    }
}
