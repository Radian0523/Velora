using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 言語切替ボタンの表示とクリックイベント発行を担当する View。
    /// ロジックは持たず、Presenter から受け取った表示名をラベルに反映するだけ。
    /// タイトル画面・ポーズメニューの両方で同じコンポーネントを使い回す。
    /// </summary>
    public class LanguageSwitchView : MonoBehaviour
    {
        [SerializeField] private Button _switchButton;
        [SerializeField] private TMP_Text _label;

        public event Action OnSwitchClicked;

        private void Awake()
        {
            _switchButton.onClick.AddListener(() => OnSwitchClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _switchButton.onClick.RemoveAllListeners();
        }

        public void SetLabel(string displayName)
        {
            _label.text = displayName;
        }
    }
}
