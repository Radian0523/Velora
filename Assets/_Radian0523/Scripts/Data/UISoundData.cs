using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// UI 効果音を一元管理する ScriptableObject。
    /// 全ボタンの SE をこの1アセットで変更できるため、
    /// 音の差し替え時にシーンやプレハブの編集が不要になる。
    /// </summary>
    [CreateAssetMenu(fileName = "UISoundData", menuName = "Velora/UI Sound Data")]
    public class UISoundData : ScriptableObject
    {
        [SerializeField] private AudioClip _buttonClick;

        public AudioClip ButtonClick => _buttonClick;
    }
}
