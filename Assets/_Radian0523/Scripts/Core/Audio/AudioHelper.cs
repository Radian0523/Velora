using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// AudioManager へのショートカット。
    /// CommonUIDirector 経由の null チェーンを一箇所に集約し、
    /// 呼び出し側のコードを簡潔にする。
    /// AudioManager のアクセス方法が変わった場合もこのクラスのみ修正すればよい。
    /// </summary>
    public static class AudioHelper
    {
        public static void PlaySE(AudioClip clip)
        {
            if (clip == null) return;
            CommonUIDirector.Instance?.AudioManager?.PlaySE(clip);
        }

        public static void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            CommonUIDirector.Instance?.AudioManager?.PlayBGM(clip).Forget();
        }

        public static void StopBGM()
        {
            CommonUIDirector.Instance?.AudioManager?.StopBGM().Forget();
        }
    }
}
