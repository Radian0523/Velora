using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// SceneManager によるシーンのロード/アンロードを管理する。
    /// フェード付きシーン遷移を UniTask の async/await で記述し、
    /// 「フェードアウト → シーン切替 → フェードイン」のシーケンスを可読性高く実現する。
    ///
    /// Additive モードで CommonUI を常駐させつつ、メインシーンを差し替える構成に対応。
    /// ロード済みシーンを追跡し、遷移時に旧シーンを正しくアンロードする。
    /// </summary>
    public class SceneLoader
    {
        private readonly HashSet<string> _loadedScenes = new();

        /// <summary>
        /// シーンをロードする。
        /// AsyncOperation.isDone をポーリングすることで確実に完了を待機する。
        /// </summary>
        public async UniTask LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            await UniTask.WaitUntil(() => op.isDone);
            _loadedScenes.Add(sceneName);
        }

        /// <summary>
        /// Additive でロードしたシーンをアンロードする。
        /// </summary>
        public async UniTask UnloadScene(string sceneName)
        {
            if (_loadedScenes.Contains(sceneName))
            {
                var op = SceneManager.UnloadSceneAsync(sceneName);
                await UniTask.WaitUntil(() => op.isDone);
                _loadedScenes.Remove(sceneName);
            }
        }

        /// <summary>
        /// フェード付きシーン遷移。
        /// fadeOut → 旧シーン Unload → 新シーン Load → fadeIn の順で実行する。
        /// fadeOut / fadeIn は呼び出し側が UniTask を返すデリゲートとして渡す。
        /// これにより FadeView への直接依存を避け、SceneLoader を純粋なサービスに保つ。
        /// </summary>
        public async UniTask TransitionTo(
            string newSceneName,
            System.Func<UniTask> fadeOut,
            System.Func<UniTask> fadeIn,
            string currentSceneName = null)
        {
            if (fadeOut != null)
            {
                await fadeOut();
            }

            if (currentSceneName != null)
            {
                await UnloadScene(currentSceneName);
            }

            await LoadScene(newSceneName, LoadSceneMode.Additive);

            if (fadeIn != null)
            {
                await fadeIn();
            }
        }
    }
}
