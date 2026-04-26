using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// 反動パターンの ScriptableObject。
    /// AnimationCurve で射撃回数に応じた反動の強さを定義する。
    /// 武器ごとに異なるリコイルパターンを Inspector から直感的に調整可能。
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecoil", menuName = "Velora/Recoil Data")]
    public class RecoilData : ScriptableObject
    {
        [Header("反動カーブ（横軸: 連射の進行度 0〜1、縦軸: 反動の強さ）")]
        [SerializeField] private AnimationCurve _verticalRecoil = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);
        [SerializeField] private AnimationCurve _horizontalRecoil = AnimationCurve.Linear(0f, -0.5f, 1f, 0.5f);

        [Header("反動復帰")]
        [SerializeField] private float _recoverySpeed = 5f;

        public AnimationCurve VerticalRecoil => _verticalRecoil;
        public AnimationCurve HorizontalRecoil => _horizontalRecoil;
        public float RecoverySpeed => _recoverySpeed;
    }
}
