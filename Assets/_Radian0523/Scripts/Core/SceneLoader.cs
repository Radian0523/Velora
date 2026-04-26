using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Velora.Core
{
    /// <summary>
    /// Addressables によるシーンのロード/アンロードを管理する。
    /// フェード付きシーン遷移を UniTask の async/await で記述し、
    /// 「フェードアウト → シーン切替 → フェードイン」のシーケンスを可読性高く実現する。
    /// </summary>
    public class SceneLoader
    {
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _loadedScenes = new();

        /// <summary>
        /// Addressables でシーンをロードする。
        /// Additive モードで CommonUIScene を常駐させつつ、メインシーンを差し替える構成に対応。
        /// </summary>
        public async UniTask LoadScene(string sceneAddress, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var handle = Addressables.LoadSceneAsync(sceneAddress, mode);
            await handle.ToUniTask();
            _loadedScenes[sceneAddress] = handle;
        }

        /// <summary>
        /// Additive でロードしたシーンをアンロードする。
        /// </summary>
        public async UniTask UnloadScene(string sceneAddress)
        {
            if (_loadedScenes.TryGetValue(sceneAddress, out var handle))
            {
                await Addressables.UnloadSceneAsync(handle).ToUniTask();
                _loadedScenes.Remove(sceneAddress);
            }
        }

        /// <summary>
        /// フェード付きシーン遷移。
        /// fadeOut → 旧シーン Unload → 新シーン Load → fadeIn の順で実行する。
        /// fadeOut / fadeIn は呼び出し側が UniTask を返すデリゲートとして渡す。
        /// これにより FadeView への直接依存を避け、SceneLoader を純粋なサービスに保つ。
        /// </summary>
        public async UniTask TransitionTo(
            string newSceneAddress,
            System.Func<UniTask> fadeOut,
            System.Func<UniTask> fadeIn,
            string currentSceneAddress = null)
        {
            if (fadeOut != null)
            {
                await fadeOut();
            }

            if (currentSceneAddress != null)
            {
                await UnloadScene(currentSceneAddress);
            }

            await LoadScene(newSceneAddress, LoadSceneMode.Additive);

            if (fadeIn != null)
            {
                await fadeIn();
            }
        }
    }
}
