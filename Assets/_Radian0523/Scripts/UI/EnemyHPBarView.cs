using UnityEngine;
using UnityEngine.UI;
using Velora.Enemy;

namespace Velora.UI
{
    /// <summary>
    /// 敵の頭上に表示するワールドスペース HP バー（View 層）。
    /// EnemyController が Initialize / Cleanup を呼び出し、
    /// EnemyModel.OnHealthChanged を購読して RectTransform の幅を更新する。
    /// ビルボード処理で常にカメラ正面を向く。
    /// </summary>
    public class EnemyHPBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        private EnemyModel _model;
        private RectTransform _fillRect;

        /// <summary>
        /// EnemyController.Initialize から呼び出す。
        /// Model のイベントを購読し、HP バーを満タン状態にリセットする。
        /// </summary>
        public void Initialize(EnemyModel model)
        {
            _model = model;
            _fillRect = _fillImage.rectTransform;
            _model.OnHealthChanged += HandleHealthChanged;
            SetFillNormalized(1f);
        }

        /// <summary>
        /// EnemyController.ReturnToPool から呼び出す。
        /// プール返却前にイベント購読を解除し、メモリリークを防止する。
        /// </summary>
        public void Cleanup()
        {
            if (_model != null)
            {
                _model.OnHealthChanged -= HandleHealthChanged;
                _model = null;
            }
        }

        private void LateUpdate()
        {
            // ビルボード：カメラの forward と同じ向きにすることで、
            // どの角度から見ても HP バーが正面を向く
            var cam = Camera.main;
            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            float normalized = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            SetFillNormalized(normalized);
        }

        /// <summary>
        /// anchorMax.x を 0〜1 で変化させることで HP バーの幅を制御する。
        /// fillAmount はスプライト未設定の Image では見た目に反映されないため、
        /// RectTransform のアンカーを直接操作する方式を採用している。
        /// </summary>
        private void SetFillNormalized(float normalized)
        {
            _fillRect.anchorMax = new Vector2(normalized, _fillRect.anchorMax.y);
        }
    }
}
