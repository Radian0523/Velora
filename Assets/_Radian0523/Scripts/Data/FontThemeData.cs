using TMPro;
using UnityEngine;

namespace Velora.Data
{
    public enum FontCategory
    {
        Heading,
        Body,
        Number
    }

    /// <summary>
    /// 言語ごとのフォントセットを定義する ScriptableObject。
    /// 新しい言語を追加する場合はこのアセットを1つ作成するだけでよい。
    /// FontThemeService が全アセットを言語キーで索引管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "FontTheme", menuName = "Velora/Font Theme")]
    public class FontThemeData : ScriptableObject
    {
        [SerializeField] private string _languageKey;
        [SerializeField] private string _displayName;
        [SerializeField] private TMP_FontAsset _headingFont;
        [SerializeField] private TMP_FontAsset _bodyFont;
        [SerializeField] private TMP_FontAsset _numberFont;

        public string LanguageKey => _languageKey;
        public string DisplayName => _displayName;

        public TMP_FontAsset GetFont(FontCategory category)
        {
            return category switch
            {
                FontCategory.Heading => _headingFont,
                FontCategory.Body => _bodyFont,
                FontCategory.Number => _numberFont,
                _ => _bodyFont
            };
        }
    }
}
