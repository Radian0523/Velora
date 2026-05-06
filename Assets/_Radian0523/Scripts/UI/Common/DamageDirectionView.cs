using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// 被弾時にダメージ元の方向を画面中央を中心とした円弧で表示する View。
    /// FPS で一般的な、クロスヘア周辺に赤い弧が出現するスタイル。
    ///
    /// 仕組み:
    /// - 画面中央にピボット（サイズ 0 の RectTransform）を配置し、Z 回転で方向を指定
    /// - ピボットの子として薄い矩形 Image を _displayRadius だけ上方にオフセット
    /// - ピボットが回転すると Image が円周上を移動し、弧セグメントとして機能する
    /// - プレハブ不要で Awake 時に動的生成し、ラウンドロビンで再利用する
    /// </summary>
    public class DamageDirectionView : MonoBehaviour
    {
        [Header("円弧設定")]
        [SerializeField] private RectTransform _indicatorParent;
        [SerializeField] private float _displayRadius = 120f;
        [SerializeField] private float _arcWidth = 60f;
        [SerializeField] private float _arcThickness = 6f;
        [SerializeField] private Color _arcColor = new Color(0.8f, 0f, 0f, 0.9f);

        [Header("表示パラメータ")]
        [SerializeField] private float _fadeDuration = 0.8f;

        private const int MaxIndicators = 4;

        private readonly Image[] _arcImages = new Image[MaxIndicators];
        private readonly RectTransform[] _pivots = new RectTransform[MaxIndicators];
        private readonly Tween[] _fadeTweens = new Tween[MaxIndicators];
        private int _nextIndex;

        private Transform _playerTransform;
        private Transform _cameraTransform;
        private bool _isInitialized;

        /// <summary>
        /// プレイヤーとカメラの参照を外部から受け取る。
        /// BattleFlowEntryPoint が戦闘開始時に呼び出す。
        /// FindWithTag を使わず、依存の受け渡しで参照を解決する。
        /// </summary>
        public void Initialize(Transform playerTransform, Camera mainCamera)
        {
            _playerTransform = playerTransform;
            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
            _isInitialized = true;
        }

        private void Awake()
        {
            for (int i = 0; i < MaxIndicators; i++)
            {
                // ピボット: 画面中央に配置し Z 回転でダメージ方向を示す
                var pivot = new GameObject($"DamageArc_{i}");
                var pivotRect = pivot.AddComponent<RectTransform>();
                pivotRect.SetParent(_indicatorParent, false);
                pivotRect.anchorMin = pivotRect.anchorMax = new Vector2(0.5f, 0.5f);
                pivotRect.anchoredPosition = Vector2.zero;
                pivotRect.sizeDelta = Vector2.zero;
                _pivots[i] = pivotRect;

                // 弧: ピボットから _displayRadius だけオフセットした薄い矩形。
                // ピボットの回転に追従し、円周上の弧セグメントとして機能する。
                var arc = new GameObject("Arc");
                var arcImage = arc.AddComponent<Image>();
                var arcRect = arc.GetComponent<RectTransform>();
                arcRect.SetParent(pivotRect, false);
                arcRect.anchorMin = arcRect.anchorMax = new Vector2(0.5f, 0.5f);
                arcRect.anchoredPosition = new Vector2(0f, _displayRadius);
                arcRect.sizeDelta = new Vector2(_arcWidth, _arcThickness);

                var color = _arcColor;
                color.a = 0f;
                arcImage.color = color;
                _arcImages[i] = arcImage;
            }

            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < MaxIndicators; i++)
            {
                _fadeTweens[i]?.Kill();
            }

            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent e)
        {
            if (!_isInitialized || _playerTransform == null || _cameraTransform == null) return;

            float angle = CalculateDirectionAngle(e.HitPoint);
            ShowIndicator(angle);
        }

        /// <summary>
        /// ダメージ元の方向角度を算出する。
        /// プレイヤー位置からヒットポイントへの水平方向ベクトルと、
        /// カメラの水平 forward との角度差を求める。
        /// Y 成分を除去して水平面上の角度のみを扱い、高低差の影響を排除する。
        /// </summary>
        private float CalculateDirectionAngle(Vector3 hitPoint)
        {
            Vector3 playerPos = _playerTransform.position;
            Vector3 toHit = hitPoint - playerPos;
            toHit.y = 0f;

            Vector3 cameraForward = _cameraTransform.forward;
            cameraForward.y = 0f;

            if (toHit.sqrMagnitude < 0.001f || cameraForward.sqrMagnitude < 0.001f)
            {
                return 0f;
            }

            return Vector3.SignedAngle(cameraForward.normalized, toHit.normalized, Vector3.up);
        }

        /// <summary>
        /// 指定角度の円弧インジケーターを表示する。
        /// ピボットの Z 回転で方向を指定し、弧を即座に表示してフェードアウトさせる。
        /// ラウンドロビンでプール内のインジケーターを順番に再利用する。
        /// </summary>
        private void ShowIndicator(float angle)
        {
            int index = _nextIndex;
            _nextIndex = (_nextIndex + 1) % MaxIndicators;

            _fadeTweens[index]?.Kill();

            _pivots[index].localRotation = Quaternion.Euler(0f, 0f, -angle);

            var image = _arcImages[index];
            image.color = _arcColor;

            _fadeTweens[index] = DOTween.ToAlpha(
                () => image.color,
                c => image.color = c,
                0f,
                _fadeDuration
            ).SetEase(Ease.OutQuad);
        }
    }
}
