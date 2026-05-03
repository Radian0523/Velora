using UnityEngine;
using UnityEngine.UI;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// Button にアタッチするだけでクリック SE を再生する自己完結型コンポーネント。
    /// FontThemeApplier と同じパターンで、View / Presenter を変更せずに SE を追加できる。
    /// 音源は CommonUIDirector.UISoundData に一元管理されるため、
    /// SE の差し替えは ScriptableObject の編集だけで完了する。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIClickSound : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayClickSound);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (CommonUIDirector.Instance == null) return;

            var soundData = CommonUIDirector.Instance.UISoundData;
            if (soundData == null) return;

            CommonUIDirector.Instance.AudioManager.PlaySE(soundData.ButtonClick);
        }
    }
}
