using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// Title シーンのオーケストレーター。
    /// Battle シーンではカーソルがロックされるため、
    /// Title に戻った際にここで明示的に解除する。
    /// Presenter の初期化は VContainer（TitleLifetimeScope）が担うため、
    /// このクラスは入場時のカーソル設定のみを責務とする。
    /// </summary>
    public class TitleSceneDirector : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
