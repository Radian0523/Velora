using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Velora.Data;

namespace Velora.Editor
{
    /// <summary>
    /// バトル関連の全音声アサインを一覧管理するエディタウィンドウ。
    /// WeaponData / EnemyData / BattleSoundData の音声フィールドを横並びで表示し、
    /// 試聴ボタンでその場で聴き比べができる。
    /// 変更は SerializedObject 経由で即座に ScriptableObject に反映される。
    /// </summary>
    public class WeaponSoundEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private WeaponData[] _weaponDataAssets;
        private EnemyData[] _enemyDataAssets;
        private BattleSoundData[] _battleSoundDataAssets;
        private AudioClip _playingClip;

        // UnityEditor.AudioUtil は internal クラスのため、リフレクションでアクセスする。
        // エディタ内で AudioClip を試聴する公式 API が存在しないための措置。
        private static readonly Type AudioUtilType =
            typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        private static readonly MethodInfo PlayClipMethod =
            AudioUtilType?.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

        private static readonly MethodInfo StopClipsMethod =
            AudioUtilType?.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

        private const float LabelWidth = 120f;
        private const float ButtonWidth = 24f;
        private const float ClipFieldMinWidth = 140f;

        [MenuItem("Velora/Battle Sound Editor", priority = 100)]
        private static void Open()
        {
            var window = GetWindow<WeaponSoundEditor>("Battle Sounds");
            window.minSize = new Vector2(700f, 400f);
        }

        private void OnEnable()
        {
            RefreshAllAssets();
        }

        private void OnFocus()
        {
            RefreshAllAssets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawWeaponSection();
            EditorGUILayout.Space(8f);
            DrawEnemySection();
            EditorGUILayout.Space(8f);
            DrawBattleSoundSection();

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            StopPreview();
        }

        // --- データ取得 ---

        private void RefreshAllAssets()
        {
            _weaponDataAssets = FindAssets<WeaponData>("t:WeaponData");
            _enemyDataAssets = FindAssets<EnemyData>("t:EnemyData");
            _battleSoundDataAssets = FindAssets<BattleSoundData>("t:BattleSoundData");
        }

        private static T[] FindAssets<T>(string filter) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets(filter);
            var assets = new T[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return assets;
        }

        // --- ツールバー ---

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                RefreshAllAssets();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();
        }

        // --- Weapon セクション ---

        private void DrawWeaponSection()
        {
            EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
            DrawSeparator();

            if (_weaponDataAssets == null || _weaponDataAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("WeaponData が見つかりません。", MessageType.Info);
                return;
            }

            DrawSectionHeader("Name", "Fire", "Reload Start", "Reload End", "Switch");

            foreach (var weaponData in _weaponDataAssets)
            {
                if (weaponData == null) continue;
                DrawWeaponRow(weaponData);
            }
        }

        private void DrawWeaponRow(WeaponData weaponData)
        {
            var serializedObject = new SerializedObject(weaponData);
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            DrawAssetLabel(weaponData.WeaponName, weaponData);

            DrawClipField(serializedObject, "_fireSound");
            DrawClipField(serializedObject, "_reloadStartSound");
            DrawClipField(serializedObject, "_reloadEndSound");
            DrawClipField(serializedObject, "_switchSound");

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // --- Enemy セクション ---

        private void DrawEnemySection()
        {
            EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
            DrawSeparator();

            if (_enemyDataAssets == null || _enemyDataAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("EnemyData が見つかりません。", MessageType.Info);
                return;
            }

            DrawSectionHeader("Name", "Attack", "Hit", "Headshot Hit");

            foreach (var enemyData in _enemyDataAssets)
            {
                if (enemyData == null) continue;
                DrawEnemyRow(enemyData);
            }
        }

        private void DrawEnemyRow(EnemyData enemyData)
        {
            var serializedObject = new SerializedObject(enemyData);
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            DrawAssetLabel(enemyData.EnemyName, enemyData);

            DrawClipField(serializedObject, "_attackSound");
            DrawClipField(serializedObject, "_hitSound");
            DrawClipField(serializedObject, "_headshotHitSound");

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // --- BattleSound セクション ---

        private void DrawBattleSoundSection()
        {
            EditorGUILayout.LabelField("Battle (Global)", EditorStyles.boldLabel);
            DrawSeparator();

            if (_battleSoundDataAssets == null || _battleSoundDataAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("BattleSoundData が見つかりません。", MessageType.Info);
                return;
            }

            DrawSectionHeader("Asset", "Player Damage", "Player Death", "Wave Clear");

            foreach (var battleSoundData in _battleSoundDataAssets)
            {
                if (battleSoundData == null) continue;
                DrawBattleSoundRow(battleSoundData);
            }
        }

        private void DrawBattleSoundRow(BattleSoundData battleSoundData)
        {
            var serializedObject = new SerializedObject(battleSoundData);
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            DrawAssetLabel(battleSoundData.name, battleSoundData);

            DrawClipField(serializedObject, "_playerDamageSound");
            DrawClipField(serializedObject, "_playerDeathSound");
            DrawClipField(serializedObject, "_waveClearSound");

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // --- 共通描画 ---

        private void DrawSectionHeader(string labelTitle, params string[] columns)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(labelTitle, EditorStyles.miniLabel, GUILayout.Width(LabelWidth));

            foreach (var column in columns)
            {
                EditorGUILayout.LabelField(column, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetLabel(string displayName, UnityEngine.Object asset)
        {
            if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.Width(LabelWidth)))
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        private void DrawClipField(SerializedObject serializedObject, string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            var clip = property.objectReferenceValue as AudioClip;

            EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.MinWidth(ClipFieldMinWidth));

            bool isPlaying = _playingClip != null && _playingClip == clip;
            var icon = isPlaying
                ? EditorGUIUtility.IconContent("d_PreMatQuad")
                : EditorGUIUtility.IconContent("d_PlayButton");

            GUI.enabled = clip != null;
            if (GUILayout.Button(icon, GUILayout.Width(ButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                if (isPlaying)
                {
                    StopPreview();
                }
                else
                {
                    PlayPreview(clip);
                }
            }
            GUI.enabled = true;
        }

        private void PlayPreview(AudioClip clip)
        {
            if (clip == null || PlayClipMethod == null) return;

            StopPreview();
            PlayClipMethod.Invoke(null, new object[] { clip, 0, false });
            _playingClip = clip;
        }

        private void StopPreview()
        {
            StopClipsMethod?.Invoke(null, null);
            _playingClip = null;
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}
