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

            var choices = _upgradeManager.GetRandomChoices();
            _view.DisplayChoices(choices);

            // DisplayChoices() 内でカードの OnSelected 購読が開始される。
            // ここでは View 全体のイベントを購読し、選択完了のシグナルを待つ。
            _view.OnUpgradeSelected += HandleSelected;

            await _cts.Task;
        }

        private void HandleSelected(UpgradeData data)
        {
            _view.OnUpgradeSelected -= HandleSelected;
            _upgradeManager.ApplyUpgrade(data, _playerModel);
            _view.Hide();
            _cts.TrySetResult();
        }
    }
}
