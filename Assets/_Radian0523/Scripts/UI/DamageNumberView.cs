using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// EnemyDamagedEvent を購読し、ワールド座標をスクリーン変換した位置に
    /// ダメージ数字を浮かび上がらせる View 層。
    /// Instantiate を使うためガベージが発生するが、Phase 5 の範囲では許容する。
    /// 本番改善時は ObjectPool に差し替える。
    /// </summary>
    public class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _damageNumberPrefab;
        [SerializeField] private Canvas _canvas;

        [Header("アニメーション設定")]
        [SerializeField] private float _floatDistance = 80f;
        [SerializeField] private float _duration = 1f;

        [Header("カラー設定")]
        [SerializeField] private Color _normalHitColor = Color.white;
        [SerializeField] private Color _headshotColor = Color.red;

        private Camera _mainCamera;
        private RectTransform _canvasRect;

        private void Start()
        {
            _mainCamera = Camera.main;
            _canvasRect = _canvas.GetComponent<RectTransform>();
            EventBus.Subscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
        }

        private void HandleEnemyDamaged(EnemyDamagedEvent e)
        {
            SpawnDamageNumber(e.Damage, e.HitPosition, e.IsHeadshot);
        }

        private void SpawnDamageNumber(float damage, Vector3 worldPosition, bool isHeadshot)
        {
            if (_mainCamera == null || _damageNumberPrefab == null) return;

            var instance = Instantiate(_damageNumberPrefab, _canvas.transform);
            instance.text = Mathf.RoundToInt(damage).ToString();
            instance.color = isHeadshot ? _headshotColor : _normalHitColor;

            // WorldToScreenPoint でスクリーン座標を得て Canvas のローカル座標に変換する。
            // Screen Space Overlay Canvas では Camera.main を null で渡す。
            Vector2 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCamera,
                out Vector2 localPoint);

            var rect = instance.rectTransform;
            rect.anchoredPosition = localPoint;

            // 上方向に浮き上がりながらフェードアウト
            rect.DOAnchorPosY(localPoint.y + _floatDistance, _duration)
                .SetEase(Ease.OutCubic);

            instance.DOFade(0f, _duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => Destroy(instance.gameObject));
        }
    }
}
