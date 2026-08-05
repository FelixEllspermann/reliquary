using System;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>Eine Zeile im Deck-Editor: Typ-Streifen, Name, Werte, Deck-Anzahl, +/− sowie Craft/Entcraft.</summary>
    public class DeckEditorRow : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Referenzen (im Prefab verdrahtet)")]
        [SerializeField] private Image typeStripe;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button dustButton;

        public CardDefinition Card { get; private set; }

        /// <summary>Welches Exemplar diese Zeile meint.</summary>
        public CardFinish Finish { get; private set; }

        private Action<CardDefinition, CardFinish> onPlus;
        private Action<CardDefinition, CardFinish> onMinus;
        private Action<CardDefinition> onHover;
        private Action<CardDefinition> onCraft;
        private Action<CardDefinition> onDust;

        private void Awake()
        {
            if (plusButton != null) plusButton.onClick.AddListener(() => onPlus?.Invoke(Card, Finish));
            if (minusButton != null) minusButton.onClick.AddListener(() => onMinus?.Invoke(Card, Finish));
            if (craftButton != null) craftButton.onClick.AddListener(() => onCraft?.Invoke(Card));
            if (dustButton != null) dustButton.onClick.AddListener(() => onDust?.Invoke(Card));
        }

        /// <summary>
        /// Eine Zeile steht für ein EXEMPLAR-Bündel: dieselbe Karte in demselben
        /// Finish. Wer drei schlichte und zwei Static besitzt, sieht zwei Zeilen und
        /// kann gezielt die eine oder die andere ins Deck legen.
        /// </summary>
        public void Setup(CardDefinition card, CardFinish finish, int deckCount, int maxCopies, int owned,
            bool collectionMode, bool canCraft, int copiesOfCardInDeck,
            Action<CardDefinition, CardFinish> plus, Action<CardDefinition, CardFinish> minus,
            Action<CardDefinition> hover, Action<CardDefinition> craft, Action<CardDefinition> dust)
        {
            Card = card;
            Finish = finish;
            onPlus = plus;
            onMinus = minus;
            onHover = hover;
            onCraft = craft;
            onDust = dust;

            if (typeStripe != null)
                typeStripe.color = finish == CardFinish.Plain ? card.FrameColor : CardFinishInfo.Accent(finish);
            if (nameText != null)
            {
                nameText.text = finish == CardFinish.Plain
                    ? card.cardName
                    : $"{card.cardName}  <color=#{ColorUtility.ToHtmlStringRGB(CardFinishInfo.Accent(finish))}>"
                      + $"{CardFinishInfo.Glyph(finish)} {CardFinishInfo.Label(finish)}</color>";
                nameText.color = CardDefinition.RarityColor(card.rarity);
            }
            if (infoText != null)
            {
                string ownedInfo = collectionMode ? $" · Owned {owned}" : "";
                infoText.text = BuildInfo(card) + ownedInfo;
            }
            if (countText != null) countText.text = deckCount.ToString();

            // Zwei Grenzen: das Kopienlimit zählt ALLE Finishes derselben Karte
            // zusammen, der Besitz gilt nur für dieses eine Finish.
            bool unlocked = !collectionMode || owned > 0;
            bool hasSpare = !collectionMode || deckCount < owned;
            if (plusButton != null)
                plusButton.interactable = copiesOfCardInDeck < maxCopies && unlocked && hasSpare;
            if (minusButton != null) minusButton.interactable = deckCount > 0;

            // Craft und Dust gehören zur Karte, nicht zum Exemplar — nur in der
            // schlichten Zeile anbieten, sonst stünde derselbe Knopf mehrfach da.
            bool plainRow = finish == CardFinish.Plain;
            if (craftButton != null)
            {
                craftButton.gameObject.SetActive(collectionMode && plainRow);
                craftButton.interactable = canCraft;
            }
            if (dustButton != null)
            {
                dustButton.gameObject.SetActive(collectionMode && plainRow);
                dustButton.interactable = owned > 0;
            }
        }

        private static string BuildInfo(CardDefinition card)
        {
            switch (card)
            {
                case MonsterCardData monster:
                    return $"{monster.AttributeTypeRichText()} · Lv {monster.level} · {monster.atk}/{monster.def}";
                case SpellCardData spell:
                    return spell.speed == SpellSpeed.Quick ? "Quick Spell" : "Spell";
                case ArtifactCardData artifact:
                    return $"Artifact · {TcgCardView.ArtifactSlotName(artifact.slot)}";
                case PlayerCardData _:
                    return "Player Card";
                default:
                    return "";
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke(Card);
    }
}
