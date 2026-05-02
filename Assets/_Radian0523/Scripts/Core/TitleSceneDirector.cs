using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// Title シーンのオーケストレーター。
    /// カーソル表示の設定、Presenter の初期化、フェードインを行う。
    /// Battle シーンではカーソルがロックされるため、
    /// Title に戻った際にここで明示的に解除する。
    /// </summary>
    public class TitleSceneDirector : MonoBehaviour
    {
        [SerializeField] private TitlePresenter _titlePresenter;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _titlePresenter.Initialize();

            if (CommonUIDirector.Instance != null)
            {
                CommonUIDirector.Instance.FadeView.FadeIn().Forget();
            }
        }
    }
}
