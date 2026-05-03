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
    /// Inspector から見た目を調整でき、シーン上の手動セットアップは不要。
    /// </summary>
    public class DamageVignetteView : MonoBehaviour
    {
        [Header("ビネット設定")]
        [SerializeField] private Color _color = new Color(0.88f, 0.22f, 0.22f);
        [SerializeField] private float _intensity = 0.45f;
        [SerializeField] private float _smoothness = 0.4f;

        [Header("アニメーション設定")]
        [SerializeField] private float _flashDuration = 0.4f;

        private Volume _volume;
        private VolumeProfile _runtimeProfile;
        private Tween _tween;

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
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);

            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
            }
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent e)
        {
            _tween?.Kill();

            // 即座に weight=1 で Vignette を全適用し、フェードアウトで減衰させる。
            _volume.weight = 1f;

            _tween = DOTween.To(
                () => _volume.weight,
                x => _volume.weight = x,
                0f,
                _flashDuration
            ).SetEase(Ease.OutQuad);
        }
    }
}
