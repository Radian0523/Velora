using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// TitleView のイベントを受け取り、シーン遷移を発火する Presenter。
    /// 連打防止のためボタン無効化→遷移→の順で処理する。
    /// </summary>
    public class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private TitleView _view;

        public void Initialize()
        {
            _view.OnStartClicked += HandleStart;
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.OnStartClicked -= HandleStart;
            }
        }

        private void HandleStart()
        {
            _view.SetStartButtonInteractable(false);
            TransitionToBattle().Forget();
        }

        private async UniTaskVoid TransitionToBattle()
        {
            if (CommonUIDirector.Instance != null)
            {
                await CommonUIDirector.Instance.TransitionToScene("Battle", "Title");
            }
        }
    }
}
