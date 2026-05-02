using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// バトル中の HUD を表示する View 層。
    /// HP・弾数・ウェーブ数・武器バーを保持し、ロジックを一切持たない。
    /// Presenter からのデータを受け取って表示するだけの責務。
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("弾数")]
        [SerializeField] private TextMeshProUGUI _ammoText;

        [Header("ウェーブ")]
        [SerializeField] private TextMeshProUGUI _waveText;

        [Header("武器バー")]
        [SerializeField] private WeaponBarView _weaponBar;

        private const float HealthSliderAnimDuration = 0.3f;

        public void UpdateHealthBar(float current, float max)
        {
            float normalized = max > 0f ? current / max : 0f;

            // スライダーを DOTween でアニメーションさせることで
            // HP 変化が視覚的にわかりやすくなり、いきなり変化する違和感を防ぐ
            _healthSlider.DOValue(normalized, HealthSliderAnimDuration).SetEase(Ease.OutQuad);

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        public void UpdateAmmoDisplay(int current, int max, bool isReloading)
        {
            _ammoText.text = isReloading ? "RELOADING..." : $"{current} / {max}";
        }

        public void ShowWaveNumber(int waveNumber)
        {
            _waveText.text = $"WAVE {waveNumber}";
        }

        public void InitializeWeaponBar()
        {
            _weaponBar.Initialize();
        }

        public void AssignWeaponToSlot(Sprite icon)
        {
            _weaponBar.AssignWeapon(icon);
        }

        public void SelectWeaponSlot(int index)
        {
            _weaponBar.SelectSlot(index);
        }
    }
}
