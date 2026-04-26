using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 画面中央にクロスヘアを表示する View。
    /// Canvas の Render Mode は Screen Space - Overlay で、
    /// 解像度に依存しない固定サイズ表示にする。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CrosshairView : MonoBehaviour
    {
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetVisible(bool isVisible)
        {
            _image.enabled = isVisible;
        }
    }
}
