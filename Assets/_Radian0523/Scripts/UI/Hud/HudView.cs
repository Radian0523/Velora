using TMPro;
using UnityEngine;

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
        [SerializeField] private HealthBarView _healthBar;

        [Header("弾数")]
        [SerializeField] private TextMeshProUGUI _ammoText;

        [Header("ウェーブ")]
        [SerializeField] private TextMeshProUGUI _waveText;

        [Header("武器バー")]
        [SerializeField] private WeaponBarView _weaponBar;

        [Header("リロードリング")]
        [SerializeField] private ReloadRingView _reloadRing;

        public void UpdateHealthBar(float current, float max)
        {
            _healthBar.UpdateHealth(current, max);
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

        public void ShowReloadRing(float duration)
        {
            _reloadRing.StartFill(duration);
        }

        public void HideReloadRing()
        {
            _reloadRing.Cancel();
        }
    }
}
