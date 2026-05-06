using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// SE 再生のショートカット。
    /// CommonUIDirector.Instance?.AudioManager?.PlaySE() の null チェーンを
    /// 一箇所に集約し、呼び出し側のコードを簡潔にする。
    /// AudioManager のアクセス方法が変わった場合もこのクラスのみ修正すればよい。
    /// </summary>
    public static class AudioHelper
    {
        public static void PlaySE(AudioClip clip)
        {
            if (clip == null) return;
            CommonUIDirector.Instance?.AudioManager?.PlaySE(clip);
        }
    }
}
