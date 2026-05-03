using TMPro;
using UnityEngine;
using Velora.Core;
using Velora.Data;

namespace Velora.UI
{
    /// <summary>
    /// TMP コンポーネントにフォントテーマを自動適用する。
    /// 各 TMP_Text が付いた GameObject に追加し、カテゴリを指定するだけで
    /// 言語切替時にフォントが自動更新される。
    ///
    /// OnEnable/OnDisable で EventBus を購読・解除するため、
    /// ObjectPool で再利用される UI（DamageNumber 等）にも安全に対応する。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FontThemeApplier : MonoBehaviour
    {
        [SerializeField] private FontCategory _category = FontCategory.Body;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FontThemeChangedEvent>(OnThemeChanged);
            ApplyFont();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FontThemeChangedEvent>(OnThemeChanged);
        }

        private void OnThemeChanged(FontThemeChangedEvent _)
        {
            ApplyFont();
        }

        private void ApplyFont()
        {
            if (CommonUIDirector.Instance == null) return;

            var font = CommonUIDirector.Instance.FontThemeService.GetFont(_category);
            if (font != null)
            {
                _text.font = font;
            }
        }
    }
}
