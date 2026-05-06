using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// TitleView のイベントを受け取り、シーン遷移を発火する Presenter。
    /// SceneNavigator を VContainer で注入することで CommonUIDirector.Instance への依存を排除。
    /// 連打防止のためボタン無効化 → 遷移 の順で処理する。
    /// </summary>
    public class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private TitleView _view;

        private SceneNavigator _sceneNavigator;

        [Inject]
        public void Construct(SceneNavigator sceneNavigator)
        {
            _sceneNavigator = sceneNavigator;
        }

        private void OnEnable()
        {
            _view.OnStartClicked += HandleStart;
        }

        private void OnDisable()
        {
            _view.OnStartClicked -= HandleStart;
        }

        private void HandleStart()
        {
            _view.SetStartButtonInteractable(false);
            TransitionToBattle().Forget();
        }

        private async UniTaskVoid TransitionToBattle()
        {
            await _sceneNavigator.TransitionTo("Battle", "Title");
        }
    }
}
