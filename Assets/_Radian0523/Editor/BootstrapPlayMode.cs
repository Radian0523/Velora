using UnityEditor;
using UnityEditor.SceneManagement;

namespace Velora.Editor
{
    /// <summary>
    /// どのシーンを開いていても、Play ボタン押下時に Bootstrap シーンから開始するエディタ拡張。
    /// EditorSceneManager.playModeStartScene に Bootstrap を設定すると、
    /// 再生開始時だけ Bootstrap に切り替わり、停止後は元のシーンに戻る。
    /// メニューからトグルで有効/無効を切り替え可能。設定は EditorPrefs に永続化される。
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapPlayMode
    {
        private const string MenuPath = "Velora/Play From Bootstrap";
        private const string PrefKey = "Velora_PlayFromBootstrap";
        private const string BootstrapScenePath = "Assets/_Radian0523/Scenes/Bootstrap.unity";

        static BootstrapPlayMode()
        {
            EditorApplication.delayCall += ApplySetting;
        }

        [MenuItem(MenuPath, priority = 100)]
        private static void Toggle()
        {
            bool current = EditorPrefs.GetBool(PrefKey, true);
            EditorPrefs.SetBool(PrefKey, !current);
            ApplySetting();
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, true));
            return true;
        }

        private static void ApplySetting()
        {
            bool enabled = EditorPrefs.GetBool(PrefKey, true);

            if (enabled)
            {
                var bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
                EditorSceneManager.playModeStartScene = bootstrapScene;
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }
    }
}
