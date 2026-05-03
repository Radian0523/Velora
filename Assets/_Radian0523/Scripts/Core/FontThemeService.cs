using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Velora.Data;

namespace Velora.Core
{
    /// <summary>
    /// フォントテーマの管理を行う pure C# クラス。
    /// AudioManager と同じパターンで、CommonUIDirector から生成・保持される。
    ///
    /// 言語ごとの FontThemeData を言語キーで索引管理し、
    /// 切替時に EventBus 経由で全 FontThemeApplier に通知する。
    /// 選択言語は PlayerPrefs で永続化する。
    /// </summary>
    public class FontThemeService
    {
        private const string LanguagePrefsKey = "FontLanguage";
        private const string DefaultLanguageKey = "ja";

        private readonly Dictionary<string, FontThemeData> _themeMap = new();
        private readonly string[] _availableLanguages;

        public FontThemeData CurrentTheme { get; private set; }
        public string[] AvailableLanguages => _availableLanguages;

        public FontThemeService(FontThemeData[] themes)
        {
            foreach (var theme in themes)
            {
                _themeMap[theme.LanguageKey] = theme;
            }

            _availableLanguages = new string[themes.Length];
            for (int i = 0; i < themes.Length; i++)
            {
                _availableLanguages[i] = themes[i].LanguageKey;
            }

            var savedKey = PlayerPrefs.GetString(LanguagePrefsKey, DefaultLanguageKey);
            // 保存された言語キーが無効な場合は先頭のテーマにフォールバック
            CurrentTheme = _themeMap.TryGetValue(savedKey, out var theme2)
                ? theme2
                : themes[0];
        }

        public void SetLanguage(string languageKey)
        {
            if (!_themeMap.TryGetValue(languageKey, out var theme)) return;
            if (CurrentTheme == theme) return;

            CurrentTheme = theme;
            PlayerPrefs.SetString(LanguagePrefsKey, languageKey);
            EventBus.Publish(new FontThemeChangedEvent(languageKey));
        }

        public TMP_FontAsset GetFont(FontCategory category)
        {
            return CurrentTheme.GetFont(category);
        }
    }
}
