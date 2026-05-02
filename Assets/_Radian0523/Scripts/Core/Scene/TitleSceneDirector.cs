using UnityEngine;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// Title シーンのオーケストレーター。
    /// カーソル表示の設定と Presenter の初期化を行う。
    /// Battle シーンではカーソルがロックされるため、
    /// Title に戻った際にここで明示的に解除する。
    ///
    /// フェード演出は CommonUIDirector.TransitionToScene が一括管理するため、
    /// ここでは呼び出さない。
    /// </summary>
    public class TitleSceneDirector : MonoBehaviour
    {
        [SerializeField] private TitlePresenter _titlePresenter;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _titlePresenter.Initialize();
        }
    }
}
