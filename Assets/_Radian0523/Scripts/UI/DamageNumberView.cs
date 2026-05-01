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
    /// ObjectPool で TextMeshProUGUI を再利用し、Instantiate/Destroy のコストを回避する。
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
        private ObjectPool<TextMeshProUGUI> _pool;

        private const int PoolInitialSize = 5;
        private const int PoolMaxSize = 20;

        private void Start()
        {
            _mainCamera = Camera.main;
            _canvasRect = _canvas.GetComponent<RectTransform>();
            _pool = new ObjectPool<TextMeshProUGUI>(
                _damageNumberPrefab, _canvas.transform, PoolInitialSize, PoolMaxSize);
            EventBus.Subscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
            _pool?.Clear();
        }

        private void HandleEnemyDamaged(EnemyDamagedEvent e)
        {
            SpawnDamageNumber(e.Damage, e.HitPosition, e.IsHeadshot);
        }

        private void SpawnDamageNumber(float damage, Vector3 worldPosition, bool isHeadshot)
        {
            if (_mainCamera == null || _pool == null) return;

            var instance = _pool.Get();

            // プールから取り出した際に前回の DOTween をキャンセルし、状態をリセットする
            instance.DOKill();
            instance.alpha = 1f;
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

            // 上方向に浮き上がりながらフェードアウトし、完了後にプールへ返却
            rect.DOAnchorPosY(localPoint.y + _floatDistance, _duration)
                .SetEase(Ease.OutCubic);

            instance.DOFade(0f, _duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => _pool.Return(instance));
        }
    }
}
