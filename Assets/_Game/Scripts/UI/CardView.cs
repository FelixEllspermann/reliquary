using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rouge.Data;

namespace Rouge.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Referenzen (im Prefab verdrahtet)")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Hover-Effekt")]
        [Range(1f, 1.5f)]
        [Tooltip("Vergrößerung der Karte, wenn die Maus darüber ist")]
        [SerializeField] private float hoverScale = 1.12f;

        public CardData Card { get; private set; }
        public event Action<CardView> Clicked;

        private Vector3 baseScale;
        private bool playable = true;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void Show(CardData card)
        {
            Card = card;
            if (card == null) return;

            if (nameText != null) nameText.text = card.cardName;
            if (costText != null) costText.text = card.cost.ToString();
            if (descriptionText != null) descriptionText.text = card.GetFormattedDescription();
            if (frameImage != null) frameImage.color = card.frameColor;
            if (artworkImage != null)
            {
                artworkImage.enabled = card.artwork != null;
                artworkImage.sprite = card.artwork;
            }
        }

        public void SetPlayable(bool value)
        {
            playable = value;
            if (canvasGroup != null) canvasGroup.alpha = value ? 1f : 0.45f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playable) Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = baseScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = baseScale;
        }
    }
}
