using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// プレイヤー被弾時に URP の Vignette で画面周囲を赤くフラッシュさせる View。
    /// Awake で専用の Volume と VolumeProfile をランタイム生成するため、
    /// 既存の Global Volume（Bloom, Tonemapping 等）と完全に分離される。
    /// 瀕死時は常時パルスに切り替わり、HP の危機感を画面全体で伝える。
    /// </summary>
    public class DamageVignetteView : MonoBehaviour
    {
        [Header("ビネット設定")]
        [SerializeField] private Color _color = new Color(0.88f, 0.22f, 0.22f);
        [SerializeField] private float _intensity = 0.45f;
        [SerializeField] private float _smoothness = 0.4f;

        [Header("ダメージフラッシュ")]
        [SerializeField] private float _flashDuration = 0.4f;

        [Header("瀕死演出")]
        [SerializeField] private float _criticalThreshold = 0.25f;
        [SerializeField] private float _criticalPulseMin = 0.15f;
        [SerializeField] private float _criticalPulseMax = 0.4f;
        [SerializeField] private float _criticalPulseDuration = 0.8f;

        private Volume _volume;
        private VolumeProfile _runtimeProfile;
        private Tween _flashTween;
        private Tween _criticalTween;
        private bool _isCritical;

        private void Awake()
        {
            // 既存の Global Volume と分離するため、専用の Volume + Profile をランタイムで構築する。
            // これにより Bloom や Tonemapping 等の他エフェクトの weight に影響しない。
            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            var vignette = _runtimeProfile.Add<Vignette>();
            vignette.color.Override(_color);
            vignette.intensity.Override(_intensity);
            vignette.smoothness.Override(_smoothness);

            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 200f;
            _volume.profile = _runtimeProfile;
            _volume.weight = 0f;
        }

        private void Start()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Subscribe<PlayerHealthChangedEvent>(HandlePlayerHealthChanged);
        }

        private void OnDestroy()
        {
            _flashTween?.Kill();
            _criticalTween?.Kill();
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(HandlePlayerHealthChanged);

            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
            }
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent e)
        {
            _flashTween?.Kill();
            _criticalTween?.Kill();
            _criticalTween = null;

            // 即座に weight=1 で Vignette を全適用し、フェードアウトで減衰させる。
            // フラッシュ完了後、瀕死状態ならパルスに移行する。
            _volume.weight = 1f;

            _flashTween = DOTween.To(
                () => _volume.weight,
                x => _volume.weight = x,
                0f,
                _flashDuration
            ).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (_isCritical) StartCriticalPulse();
            });
        }

        private void HandlePlayerHealthChanged(PlayerHealthChangedEvent e)
        {
            float normalized = e.Max > 0f ? e.Current / e.Max : 0f;
            bool wasCritical = _isCritical;
            _isCritical = normalized <= _criticalThreshold && normalized > 0f;

            // ダメージフラッシュ再生中はフラッシュの OnComplete で制御するため、
            // ここではパルスの開始/停止を行わない
            if (IsFlashPlaying()) return;

            if (_isCritical && !wasCritical)
            {
                StartCriticalPulse();
            }
            else if (!_isCritical && wasCritical)
            {
                StopCriticalPulse();
            }
        }

        private bool IsFlashPlaying()
        {
            return _flashTween != null && _flashTween.IsActive() && _flashTween.IsPlaying();
        }

        /// <summary>
        /// 瀕死状態で画面周囲のビネットを低 weight で常時パルスさせ、
        /// プレイヤーに HP 残量の危機感を伝える。
        /// </summary>
        private void StartCriticalPulse()
        {
            _criticalTween?.Kill();
            _volume.weight = _criticalPulseMin;

            _criticalTween = DOTween.To(
                () => _volume.weight,
                x => _volume.weight = x,
                _criticalPulseMax,
                _criticalPulseDuration * 0.5f
            ).SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopCriticalPulse()
        {
            _criticalTween?.Kill();

            // 瀕死解除時にビネットを滑らかにフェードアウトさせる
            _criticalTween = DOTween.To(
                () => _volume.weight,
                x => _volume.weight = x,
                0f,
                _flashDuration * 0.5f
            ).SetEase(Ease.OutQuad)
            .OnComplete(() => _criticalTween = null);
        }
    }
}
