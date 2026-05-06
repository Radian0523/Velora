using DG.Tweening;
using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 武器モデルプレハブのルートにアタッチする View コンポーネント。
    /// マズルポイントの位置はモデルのジオメトリが決定するため、
    /// モデル側に持たせるのが自然。WeaponController は WeaponData → WeaponModelView の
    /// マッピングで各武器の 3D 表示とエフェクト位置を管理する。
    ///
    /// マズルフラッシュ・発射キックなど、武器モデルに直結した視覚フィードバックも
    /// このクラスで担うことで、WeaponController から演出責務を分離する。
    /// 武器ごとに異なるエフェクトを設定できるため、データドリブンな拡張が可能。
    /// </summary>
    public class WeaponModelView : MonoBehaviour
    {
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private ParticleSystem _muzzleFlashVfx;

        [Header("装填弾ビジュアル（任意）")]
        [SerializeField] private GameObject _loadedAmmoVisual;

        private Vector3 _restLocalPosition;

        public Transform MuzzlePoint => _muzzlePoint;
        public bool HasLoadedAmmoVisual => _loadedAmmoVisual != null;

        /// <summary>
        /// Instantiate 直後の localPosition を記録し、
        /// 武器切替アニメーション後の復帰位置として使う。
        /// </summary>
        public Vector3 RestLocalPosition => _restLocalPosition;

        private void Awake()
        {
            _restLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// 装填弾の表示/非表示を切り替える。
        /// ロケットランチャーなど、発射時に弾が見えなくなり
        /// リロード完了で再表示される武器で使用する。
        /// _loadedAmmoVisual 未設定の武器では何もしない。
        /// </summary>
        public void SetLoadedAmmoVisible(bool visible)
        {
            if (_loadedAmmoVisual == null) return;
            _loadedAmmoVisual.SetActive(visible);
        }

        /// <summary>
        /// マズルフラッシュを再生する。
        /// VFX は武器モデルの銃口に配置済みのため、WeaponController 側での
        /// transform 移動が不要になる。武器ごとに異なる VFX を設定できる。
        /// </summary>
        public void PlayMuzzleFlash()
        {
            _muzzleFlashVfx?.Play();
        }

        /// <summary>
        /// 発射時の武器モデルキック演出。
        /// DOTween の Punch で後退 + 上方向回転を同時適用し、自然な反動を再現する。
        /// パラメータは WeaponData で武器ごとに調整可能（データドリブン）。
        /// 射撃ロジックは WeaponController が担うが、演出は View が担当するため
        /// このクラスに配置することで責務の分離を明確にする。
        /// </summary>
        public void PlayKick(WeaponData weaponData)
        {
            if (weaponData == null) return;

            transform.DOComplete();

            transform.DOPunchPosition(
                Vector3.back * weaponData.KickBackDistance,
                weaponData.KickDuration,
                weaponData.KickVibrato);

            transform.DOPunchRotation(
                Vector3.right * -weaponData.KickUpAngle,
                weaponData.KickDuration,
                weaponData.KickVibrato);
        }
    }
}
