using UnityEngine;

namespace Velora.Weapon
{
    /// <summary>
    /// 武器モデルプレハブのルートにアタッチする View コンポーネント。
    /// マズルポイントの位置はモデルのジオメトリが決定するため、
    /// モデル側に持たせるのが自然。WeaponController は WeaponData → WeaponModelView の
    /// マッピングで各武器の 3D 表示とエフェクト位置を管理する。
    /// </summary>
    public class WeaponModelView : MonoBehaviour
    {
        [SerializeField] private Transform _muzzlePoint;

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
    }
}
