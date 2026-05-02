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

        private Vector3 _restLocalPosition;

        public Transform MuzzlePoint => _muzzlePoint;

        /// <summary>
        /// Instantiate 直後の localPosition を記録し、
        /// 武器切替アニメーション後の復帰位置として使う。
        /// </summary>
        public Vector3 RestLocalPosition => _restLocalPosition;

        private void Awake()
        {
            _restLocalPosition = transform.localPosition;
        }
    }
}
