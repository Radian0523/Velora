using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Velora.Data;

namespace Velora.UI
{
    /// <summary>
    /// アップグレード選択パネルの View 層。
    /// 3枚のカードをスライドイン演出で表示し、選択イベントを上位に通知する。
    /// カードの OnSelected 購読は DisplayChoices 内で開始し、Hide 時に解除する。
    /// これにより「DisplayChoices を呼べば UI が正しく機能する」という一貫性を保つ。
    /// </summary>
    public class UpgradeSelectView : MonoBehaviour
    {
        [SerializeField] private UpgradeCardView[] _cardViews;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("アニメーション設定")]
        [SerializeField] private float _slideInDistance = 200f;
        [SerializeField] private float _slideInDuration = 0.4f;
        [SerializeField] private float _cardStagger = 0.08f;
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.25f;

        // 元の anchoredPosition を保持して複数回の表示に対応する
        private Vector2[] _cardOriginalPositions;

        public event Action<UpgradeData> OnUpgradeSelected;

        private void Awake()
        {
            _cardOriginalPositions = new Vector2[_cardViews.Length];
            for (int i = 0; i < _cardViews.Length; i++)
            {
                _cardOriginalPositions[i] = _cardViews[i].GetComponent<RectTransform>().anchoredPosition;
            }
        }

        /// <summary>
        /// カード選択肢を表示してスライドイン演出を開始する。
        /// 演出は非同期で進行するが、このメソッド自体は await しない。
        /// Presenter は OnUpgradeSelected イベントを介して選択完了を検知する。
        /// </summary>
        public void DisplayChoices(IReadOnlyList<UpgradeData> choices)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _fadeInDuration);

            int cardCount = Mathf.Min(_cardViews.Length, choices.Count);
            for (int i = 0; i < cardCount; i++)
            {
                var cardView = _cardViews[i];
                cardView.gameObject.SetActive(true);
                cardView.Setup(choices[i]);

                // 購読はここで開始し、Hide() で解除する。
                // 毎回 DisplayChoices が呼ばれるたびに購読を貼り直すため、
                // Hide() での解除と対になっていることを維持すること。
                cardView.OnSelected += HandleCardSelected;

                // カードを下にオフセットしてからスライドイン
                var rect = cardView.GetComponent<RectTransform>();
                rect.anchoredPosition = _cardOriginalPositions[i] + Vector2.down * _slideInDistance;
                rect.DOAnchorPos(_cardOriginalPositions[i], _slideInDuration)
                    .SetDelay(i * _cardStagger)
                    .SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// パネルをフェードアウトして非アクティブにする。
        /// カードの OnSelected 購読もここで解除する。
        /// </summary>
        public void Hide()
        {
            foreach (var card in _cardViews)
            {
                card.OnSelected -= HandleCardSelected;
            }

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOFade(0f, _fadeOutDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void HandleCardSelected(UpgradeData data)
        {
            OnUpgradeSelected?.Invoke(data);
        }
    }
}
