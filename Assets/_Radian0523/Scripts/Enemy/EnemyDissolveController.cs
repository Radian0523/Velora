using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Velora.Enemy
{
    /// <summary>
    /// ディゾルブ演出の View 層コンポーネント。
    /// 通常時は元マテリアルで描画し、死亡演出時のみディゾルブマテリアルに差し替える。
    /// プール返却時に元マテリアルへ復元する。
    /// MaterialPropertyBlock で _DissolveAmount を制御し、マテリアルインスタンスのリーク管理を不要にする。
    /// </summary>
    public class EnemyDissolveController : MonoBehaviour
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        [SerializeField] private Renderer[] _renderers;

        // 元マテリアル → ディゾルブマテリアルの対応を Inspector で設定する。
        // レンダラーごとにマテリアル数が異なるケース（R1_Head は 3 スロット）にも対応可能。
        [SerializeField] private DissolveMaterialMapping[] _materialMappings;

        private MaterialPropertyBlock _propertyBlock;

        // プール再利用時に復元するため、元マテリアルをレンダラーごとにキャッシュする。
        private Material[][] _originalMaterials;

        // SwapToDissolve で毎フレーム生成しないよう、レンダラーごとのディゾルブ配列を事前構築する。
        private Material[][] _dissolveMaterials;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CacheOriginalMaterials();
            BuildDissolveMaterialArrays();
        }

        /// <summary>
        /// 元マテリアルに復元し、PropertyBlock をクリアする。
        /// プール再利用時に EnemyController.Initialize から呼び出す。
        /// </summary>
        public void ResetDissolve()
        {
            RestoreOriginalMaterials();
            _propertyBlock.Clear();
            ApplyPropertyBlock();
        }

        /// <summary>
        /// ディゾルブ演出を再生する。
        /// マテリアルをディゾルブ用に差し替えた後、_DissolveAmount を 0→1 に補間する。
        /// </summary>
        public async UniTask PlayDissolve(float duration, CancellationToken cancellationToken)
        {
            SwapToDissolve();

            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float amount = Mathf.Clamp01(elapsed / duration);

                _propertyBlock.SetFloat(DissolveAmountId, amount);
                ApplyPropertyBlock();

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _propertyBlock.SetFloat(DissolveAmountId, 1f);
            ApplyPropertyBlock();
        }

        private void CacheOriginalMaterials()
        {
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _originalMaterials[i] = _renderers[i].sharedMaterials;
            }
        }

        /// <summary>
        /// レンダラーごとのディゾルブマテリアル配列を事前構築する。
        /// 元マテリアルのスロット数が異なっていても対応できるよう、
        /// _materialMappings から元→ディゾルブの対応を引いて配列を組む。
        /// </summary>
        private void BuildDissolveMaterialArrays()
        {
            _dissolveMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_originalMaterials[i] == null) continue;

                var originals = _originalMaterials[i];
                var dissolves = new Material[originals.Length];

                for (int slot = 0; slot < originals.Length; slot++)
                {
                    dissolves[slot] = FindDissolveVariant(originals[slot]);
                }

                _dissolveMaterials[i] = dissolves;
            }
        }

        private Material FindDissolveVariant(Material original)
        {
            if (original == null) return null;

            foreach (var mapping in _materialMappings)
            {
                if (mapping.Original == original) return mapping.Dissolve;
            }

            // マッピングが見つからない場合は元マテリアルをそのまま使用する。
            // ディゾルブ対象外のスロットは通常描画のまま残る。
            return original;
        }

        private void SwapToDissolve()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null || _dissolveMaterials[i] == null) continue;
                _renderers[i].sharedMaterials = _dissolveMaterials[i];
            }
        }

        private void RestoreOriginalMaterials()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null || _originalMaterials[i] == null) continue;
                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void ApplyPropertyBlock()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                for (int slot = 0; slot < _renderers[i].sharedMaterials.Length; slot++)
                {
                    _renderers[i].SetPropertyBlock(_propertyBlock, slot);
                }
            }
        }
    }

    /// <summary>
    /// 元マテリアルとディゾルブマテリアルの対応。
    /// Inspector でアサインすることで、コード変更なしにマテリアル構成を変更できる。
    /// </summary>
    [System.Serializable]
    public struct DissolveMaterialMapping
    {
        [SerializeField] private Material _original;
        [SerializeField] private Material _dissolve;

        public Material Original => _original;
        public Material Dissolve => _dissolve;
    }
}
