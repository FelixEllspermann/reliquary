using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>Ein Pack-Angebot im Shop: Name, Beschreibung, Preis, Besitz, Kaufen/Öffnen.</summary>
    public class ShopPackRow : MonoBehaviour
    {
        [Header("Referenzen (im Prefab verdrahtet)")]
        [SerializeField] private Image accentStripe;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ownedText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;
        [SerializeField] private Button openButton;

        public CardPackDefinition Pack { get; private set; }

        private Action<CardPackDefinition> onBuy;
        private Action<CardPackDefinition> onOpen;

        private void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(() => onBuy?.Invoke(Pack));
            if (openButton != null) openButton.onClick.AddListener(() => onOpen?.Invoke(Pack));
        }

        public void Setup(CardPackDefinition pack, int coins, int owned,
            Action<CardPackDefinition> buy, Action<CardPackDefinition> open)
        {
            Pack = pack;
            onBuy = buy;
            onOpen = open;

            if (accentStripe != null) accentStripe.color = pack.packColor;
            if (nameText != null) nameText.text = pack.packName;
            if (descriptionText != null) descriptionText.text = pack.description;
            if (ownedText != null) ownedText.text = $"Owned: {owned}";
            if (buyLabel != null) buyLabel.text = $"Buy ({pack.price})";
            if (buyButton != null) buyButton.interactable = coins >= pack.price;
            if (openButton != null) openButton.interactable = owned > 0;
        }
    }
}
