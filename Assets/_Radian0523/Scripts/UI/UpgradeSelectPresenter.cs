using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Data;
using Velora.Player;
using Velora.Upgrade;

namespace Velora.UI
{
    /// <summary>
    /// アップグレード選択の Presenter。
    /// ShowAndWait() は UniTaskCompletionSource でカード選択まで待機し、
    /// 呼び出し元（UpgradeSelectState）に選択完了を await させる。
    /// これにより State のコードが「選択を待つ」という意図を明確に表現できる。
    ///
    /// 表示中はカーソルをアンロックし、FPSController の入力処理を自動停止させる。
    /// FPSController は Cursor.lockState を参照して入力スキップを判断するため、
    /// ここでカーソル状態を切り替えるだけで移動・視点操作が連動して停止する。
    /// </summary>
    public class UpgradeSelectPresenter : MonoBehaviour
    {
        [SerializeField] private UpgradeSelectView _view;

        private UpgradeManager _upgradeManager;
        private PlayerModel _playerModel;
        private UniTaskCompletionSource _cts;

        public void Initialize(UpgradeManager upgradeManager, PlayerModel playerModel)
        {
            _upgradeManager = upgradeManager;
            _playerModel = playerModel;
        }

        /// <summary>
        /// ウェーブクリア後にアップグレード選択 UI を表示し、
        /// プレイヤーがカードを選ぶまでこのメソッド内で待機する。
        /// </summary>
        public async UniTask ShowAndWait()
        {
            _cts = new UniTaskCompletionSource();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var choices = _upgradeManager.GetRandomChoices();
            _view.DisplayChoices(choices);
            _view.OnUpgradeSelected += HandleSelected;

            await _cts.Task;
        }

        private void HandleSelected(UpgradeData data)
        {
            _view.OnUpgradeSelected -= HandleSelected;
            _upgradeManager.ApplyUpgrade(data, _playerModel);
            _view.Hide();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _cts.TrySetResult();
        }
    }
}
