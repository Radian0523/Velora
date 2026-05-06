using System;
using UnityEngine;
using VContainer;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// LanguageSwitchView と FontThemeService を橋渡しする Presenter。
    /// ボタンクリックで言語をトグル切替し、EventBus 経由で
    /// 他シーンの LanguageSwitchPresenter とも状態を同期する。
    /// FontThemeService は VContainer から注入されるため CommonUIDirector への依存がない。
    /// </summary>
    public class LanguageSwitchPresenter : MonoBehaviour
    {
        [SerializeField] private LanguageSwitchView _view;

        private FontThemeService _fontThemeService;

        [Inject]
        public void Construct(FontThemeService fontThemeService)
        {
            _fontThemeService = fontThemeService;
        }

        private void OnEnable()
        {
            _view.OnSwitchClicked += HandleSwitch;
            EventBus.Subscribe<FontThemeChangedEvent>(OnThemeChanged);

            // 再有効化時（Construct 完了後）にラベルを最新の言語に同期する。
            // 初回有効化時は Construct が未呼び出しのため null チェックで保護する。
            if (_fontThemeService != null)
            {
                UpdateLabel();
            }
        }

        private void Start()
        {
            // Construct（[Inject]）は Awake 後・Start 前に完了するため、
            // Start では確実に _fontThemeService が設定済みになっている。
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
