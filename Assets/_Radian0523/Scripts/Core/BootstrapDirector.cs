using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Velora.Core
{
    /// <summary>
    /// ゲーム起動時の最初のシーン（Bootstrap）に配置するオーケストレーター。
    /// CommonUI（常駐 UI）を Additive ロードした後、Title シーンをロードし、
    /// 自身の Bootstrap シーンをアンロードする。
    ///
    /// この設計により全ゲームシーン（Title, Battle）が SceneManager 経由でロードされ、
    /// CommonUI は常に Additive で存在し続ける。
    ///
    /// コルーチンで実装している理由:
    /// Unity 6 + Addressables 2.9 環境では AsyncOperationHandle の await 完了に
    /// 既知の問題があるため、コルーチンの yield return による確実な待機を採用。
    /// </summary>
    public class BootstrapDirector : MonoBehaviour
    {
        private const string CommonUISceneName = "CommonUI";
        private const string TitleSceneName = "Title";

        private IEnumerator Start()
        {
            // CommonUI を Additive で先にロードし、FadeView・AudioManager 等を初期化する
            yield return SceneManager.LoadSceneAsync(CommonUISceneName, LoadSceneMode.Additive);

            // Title シーンを Additive でロード
            yield return SceneManager.LoadSceneAsync(TitleSceneName, LoadSceneMode.Additive);

            // Bootstrap シーンは役目を終えたのでアンロード。
            // この yield return 以降のコードは実行されない
            // （Bootstrap の破棄によりコルーチンが停止するため）。
            yield return SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}
