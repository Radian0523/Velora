using System;
using UnityEngine;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// LanguageSwitchView と FontThemeService を橋渡しする Presenter。
    /// ボタンクリックで言語をトグル切替し、EventBus 経由で
    /// 他シーンの LanguageSwitchPresenter とも状態を同期する。
    /// </summary>
    public class LanguageSwitchPresenter : MonoBehaviour
    {
        [SerializeField] private LanguageSwitchView _view;

        private FontThemeService _fontThemeService;

        private void OnEnable()
        {
            _view.OnSwitchClicked += HandleSwitch;
            EventBus.Subscribe<FontThemeChangedEvent>(OnThemeChanged);

            // 再有効化時（Start 完了後）にラベルを最新の言語に同期する
            if (_fontThemeService != null)
            {
                UpdateLabel();
            }
        }

        /// <summary>
        /// CommonUIDirector と同一シーン（CommonUI）に配置されるため、
        /// OnEnable 時点では Instance が未初期化の場合がある。
        /// Start は全 Awake 完了後に呼ばれるため、安全に参照を取得できる。
        /// </summary>
        private void Start()
        {
            _fontThemeService = CommonUIDirector.Instance.FontThemeService;
            UpdateLabel();
        }

        private void OnDisable()
        {
            _view.OnSwitchClicked -= HandleSwitch;
            EventBus.Unsubscribe<FontThemeChangedEvent>(OnThemeChanged);
        }

        /// <summary>
        /// 利用可能な言語リストを巡回して次の言語に切り替える。
        /// 言語が 2 種（ja / en）の場合は実質トグル動作になる。
        /// 言語が増えても巡回ロジックがそのまま対応するため拡張性を保てる。
        /// </summary>
        private void HandleSwitch()
        {
            string[] languages = _fontThemeService.AvailableLanguages;
            string currentKey = _fontThemeService.CurrentTheme.LanguageKey;

            int currentIndex = Array.IndexOf(languages, currentKey);
            int nextIndex = (currentIndex + 1) % languages.Length;

            _fontThemeService.SetLanguage(languages[nextIndex]);
        }

        private void OnThemeChanged(FontThemeChangedEvent _)
        {
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            _view.SetLabel(_fontThemeService.CurrentTheme.DisplayName);
        }
    }
}
