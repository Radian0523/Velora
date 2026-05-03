using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Velora.Data;

namespace Velora.Editor
{
    /// <summary>
    /// 全武器の音声アサインを一覧で管理するエディタウィンドウ。
    /// 武器ごとに FireSound / ReloadStartSound / ReloadEndSound を横並びで表示し、
    /// 試聴ボタンでその場で聴き比べができる。
    /// 変更は SerializedObject 経由で即座に ScriptableObject に反映される。
    /// </summary>
    public class WeaponSoundEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private WeaponData[] _weaponDataAssets;
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

        [MenuItem("Velora/Weapon Sound Editor", priority = 100)]
        private static void Open()
        {
            var window = GetWindow<WeaponSoundEditor>("Weapon Sounds");
            window.minSize = new Vector2(600f, 300f);
        }

        private void OnEnable()
        {
            RefreshWeaponDataAssets();
        }

        private void OnFocus()
        {
            RefreshWeaponDataAssets();
        }

        private void OnGUI()
        {
            if (_weaponDataAssets == null || _weaponDataAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("WeaponData が見つかりません。", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawHeader();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var weaponData in _weaponDataAssets)
            {
                if (weaponData == null) continue;
                DrawWeaponRow(weaponData);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            StopPreview();
        }

        private void RefreshWeaponDataAssets()
        {
            var guids = AssetDatabase.FindAssets("t:WeaponData");
            _weaponDataAssets = new WeaponData[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _weaponDataAssets[i] = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                RefreshWeaponDataAssets();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel, GUILayout.Width(LabelWidth));
            EditorGUILayout.LabelField("Fire", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Reload Start", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Reload End", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            DrawSeparator();
        }

        private void DrawWeaponRow(WeaponData weaponData)
        {
            var serializedObject = new SerializedObject(weaponData);
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            // 武器名ラベル（クリックで SO を選択）
            if (GUILayout.Button(weaponData.WeaponName, EditorStyles.label, GUILayout.Width(LabelWidth)))
            {
                EditorGUIUtility.PingObject(weaponData);
                Selection.activeObject = weaponData;
            }

            DrawClipField(serializedObject, "_fireSound");
            DrawClipField(serializedObject, "_reloadStartSound");
            DrawClipField(serializedObject, "_reloadEndSound");

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
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
