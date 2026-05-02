using DG.Tweening;
using UnityEngine;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// プレイヤー被弾時にカメラを揺らす演出 View。
    /// DOShakePosition は完了後に自動で元の位置に戻るため、
    /// 位置のリセット処理は不要。
    /// </summary>
    public class ScreenShakeView : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _shakeStrength = 0.3f;
        [SerializeField] private float _shakeDuration = 0.2f;

        private void Start()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent e)
        {
            // 既に揺れ中の場合は DOKill してから新しい揺れを開始し、
            // 元の位置を保持するため snapping=false（デフォルト）を使用
            _cameraTransform.DOKill();
            _cameraTransform.DOShakePosition(_shakeDuration, _shakeStrength);
        }
    }
}
