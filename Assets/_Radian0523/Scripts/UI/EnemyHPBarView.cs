using UnityEngine;
using UnityEngine.UI;
using Velora.Enemy;

namespace Velora.UI
{
    /// <summary>
    /// 敵の頭上に表示するワールドスペース HP バー（View 層）。
    /// EnemyController が Initialize / Cleanup を呼び出し、
    /// EnemyModel.OnHealthChanged を購読して fillAmount を更新する。
    /// ビルボード処理で常にカメラ正面を向く。
    /// </summary>
    public class EnemyHPBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        private EnemyModel _model;

        /// <summary>
        /// EnemyController.Initialize から呼び出す。
        /// Model のイベントを購読し、HP バーを満タン状態にリセットする。
        /// </summary>
        public void Initialize(EnemyModel model)
        {
            _model = model;
            _model.OnHealthChanged += HandleHealthChanged;
            _fillImage.fillAmount = 1f;
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
            _fillImage.fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        }
    }
}
