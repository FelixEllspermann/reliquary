using System;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Eine 62px-Karten-Zeile der Collection-Screens (Pool- und Deck-Liste):
    /// Attribut-Streifen, Rarity-gefärbter Name, Meta-Zeile, Level-Wappen,
    /// Zähler-Block und −/+ Buttons.
    ///
    /// Bedienung: einfacher Klick wählt die Karte für die Detail-Rail, Doppelklick
    /// legt sie ins Deck (Pool) bzw. nimmt sie heraus (Deck-Liste); dafür gibt es
    /// außerdem die −/+ Knöpfe. Bloßes Überfahren ändert die Auswahl nicht mehr.
    /// Deck-Zeilen lassen sich zusätzlich aus dem Deck-Panel HERAUSZIEHEN —
    /// loslassen außerhalb nimmt das Exemplar aus dem Deck (SetDragArea).
    /// </summary>
    public class CollectionRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Referenzen (vom Builder verdrahtet)")]
        [SerializeField] private Image background;
        [SerializeField] private Image frame;
        [SerializeField] private Image stripe;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private Image rarityGem;    // Raute in kräftiger Rarity-Farbe
        [SerializeField] private Image rarityGlow;   // Schein hinter der Raute (Rare/Legendary)
        [SerializeField] private Image pipImage;
        [SerializeField] private GameObject crestRoot;
        [SerializeField] private TMP_Text crestText;
        [SerializeField] private TMP_Text countValue;
        [SerializeField] private TMP_Text countCaption;
        [SerializeField] private Button minusButton;
        [SerializeField] private Image minusBg;
        [SerializeField] private Image minusFrame;
        [SerializeField] private TMP_Text minusLabel;
        [SerializeField] private Button plusButton;
        [SerializeField] private Image plusBg;
        [SerializeField] private Image plusFrame;
        [SerializeField] private TMP_Text plusLabel;

        private CardDefinition card;
        private CardFinish finish;
        private bool deckSide;
        private Action<CardDefinition, CardFinish> onAdd;
        private Action<CardDefinition, CardFinish> onRemove;
        private Action<CardDefinition, CardFinish> onSelect;
        private bool selected;
        private bool hovered;

        // ---- Herausziehen aus dem Deck (nur wenn dragArea gesetzt ist) ----
        private RectTransform dragArea;
        private GameObject dragGhost;
        private CanvasGroup dragGhostGroup;

        // ---- Design-Farben (README-collection-screens) ----
        public static Color RarityInk(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return FromHex("#9FDCBE");
                case CardRarity.Rare: return FromHex("#A6CCEA");
                case CardRarity.Legendary: return FromHex("#F3DDA4");
                default: return FromHex("#C6CCD4");
            }
        }

        /// <summary>Kräftige Rarity-Farbe für Gems/Badges (sichtbarer als die Ink-Töne).</summary>
        public static Color RarityStrong(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return FromHex("#3FCF8C");
                case CardRarity.Rare: return FromHex("#4EA5F5");
                case CardRarity.Legendary: return FromHex("#FFC24D");
                default: return FromHex("#8E98A6");
            }
        }

        public static Color TypeKeyline(CardDefinition definition)
        {
            if (definition is SpellCardData) return FromHex("#8FC6D2");
            if (definition is ArtifactCardData) return FromHex("#B9A3E0");
            return FromHex("#C8A45C");
        }

        public static Color AccentOf(CardDefinition definition)
        {
            return definition is MonsterCardData monster
                ? MonsterCardData.AttributeColor(monster.attribute)
                : TypeKeyline(definition);
        }

        private static Color FromHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        public CardDefinition Card => card;

        /// <summary>Die Ausführung, für die diese Zeile steht.</summary>
        public CardFinish Finish => finish;

        /// <summary>Farbe und Wort einer Banlist-Stufe. limit: 0 gebannt, 1 limitiert, 2 semi.</summary>
        public static string RestrictionHex(int limit) => limit <= 0 ? "E0603A" : limit == 1 ? "E8A33D" : "E8D08A";
        public static string RestrictionWord(int limit) => limit <= 0 ? "FORBIDDEN" : limit == 1 ? "LIMITED" : "SEMI-LIMITED";

        /// <summary>
        /// Eine Zeile steht für ein Exemplar-Bündel: dieselbe Karte im selben Finish.
        /// Wer drei schlichte und zwei Static besitzt, sieht zwei Zeilen und kann
        /// gezielt die eine oder die andere ins Deck legen.
        ///
        /// Zwei Grenzen, die man nicht verwechseln darf: <paramref name="maxCopies"/>
        /// und <paramref name="copiesOfCard"/> zählen ALLE Finishes der Karte
        /// zusammen (Banlist), <paramref name="owned"/> und <paramref name="inDeck"/>
        /// nur dieses eine Finish.
        /// </summary>
        public void Setup(CardDefinition definition, CardFinish cardFinish, int inDeck, int owned, int maxCopies,
            int copiesOfCard, bool isDeckSide,
            Action<CardDefinition, CardFinish> add, Action<CardDefinition, CardFinish> remove,
            Action<CardDefinition, CardFinish> select, int banLimit = -1)
        {
            card = definition;
            finish = cardFinish;
            deckSide = isDeckSide;
            onAdd = add;
            onRemove = remove;
            onSelect = select;

            bool special = cardFinish != CardFinish.Plain;
            Color accent = special ? CardFinishInfo.Accent(cardFinish) : AccentOf(definition);
            if (stripe != null) stripe.color = accent;
            if (pipImage != null) pipImage.color = accent;

            if (nameText != null)
            {
                // Banlist-Marke steht VOR dem Namen — sie ergänzt die Zeile, statt etwas zu ersetzen
                string mark = banLimit >= 0
                    ? $"<color=#{RestrictionHex(banLimit)}><b>[{banLimit}]</b></color> "
                    : "";
                string finishTag = special
                    ? $"  <color=#{ColorUtility.ToHtmlStringRGB(CardFinishInfo.Accent(cardFinish))}>"
                      + $"{CardFinishInfo.Glyph(cardFinish)} {CardFinishInfo.Label(cardFinish).ToUpperInvariant()}</color>"
                    : "";
                nameText.text = mark + definition.cardName + finishTag;
                nameText.color = RarityInk(definition.rarity);
            }

            Color strong = RarityStrong(definition.rarity);
            if (rarityGem != null) rarityGem.color = strong;
            if (rarityGlow != null)
            {
                bool shiny = definition.rarity == CardRarity.Rare || definition.rarity == CardRarity.Legendary;
                rarityGlow.gameObject.SetActive(shiny);
                if (shiny) rarityGlow.color = new Color(strong.r, strong.g, strong.b, definition.rarity == CardRarity.Legendary ? 0.5f : 0.3f);
            }

            if (metaText != null)
            {
                string restriction = banLimit >= 0
                    ? $"<color=#{RestrictionHex(banLimit)}><b>{RestrictionWord(banLimit)}</b></color> · "
                    : "";
                metaText.text = restriction + BuildMeta(definition);
            }

            bool isMonster = definition is MonsterCardData;
            if (crestRoot != null) crestRoot.SetActive(isMonster);
            if (crestText != null && isMonster) crestText.text = ((MonsterCardData)definition).level.ToString();

            if (countValue != null)
            {
                countValue.text = deckSide ? $"×{inDeck}" : inDeck.ToString();
                // Zu viele Kopien im Deck (z.B. nach einer neuen Banlist) fallen sofort auf
                bool overLimit = banLimit >= 0 && copiesOfCard > banLimit;
                countValue.color = overLimit ? FromHex("#" + RestrictionHex(banLimit))
                    : FromHex(inDeck > 0 ? "#F3DDA4" : "#5C513F");
            }
            if (countCaption != null)
            {
                string ownedWord = special ? $"{owned} {CardFinishInfo.Label(cardFinish).ToUpperInvariant()}" : $"{owned} OWNED";
                countCaption.text = banLimit >= 0
                    ? (deckSide ? $"MAX {banLimit}" : $"{ownedWord} · MAX {banLimit}")
                    : (deckSide ? "COPIES" : $"OF {ownedWord}");
            }

            // Hinzufügen braucht beides: ein freies Exemplar DIESES Finishes und
            // Luft unter dem Kopienlimit der Karte.
            bool canAdd = inDeck < owned && copiesOfCard < maxCopies;
            bool canRemove = inDeck > 0;
            StyleControl(minusButton, minusBg, minusFrame, minusLabel, canRemove, true);
            StyleControl(plusButton, plusBg, plusFrame, plusLabel, canAdd, false);
            if (minusButton != null)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(() => { onSelect?.Invoke(card, finish); onRemove?.Invoke(card, finish); });
            }
            if (plusButton != null)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(() => { onSelect?.Invoke(card, finish); onAdd?.Invoke(card, finish); });
            }

            hovered = false;
            SetSelected(false);
        }

        private static string BuildMeta(CardDefinition definition)
        {
            // Rarity immer sichtbar: farbiges Wort am Ende jeder Meta-Zeile
            string rarityWord = $"<color=#{ColorUtility.ToHtmlStringRGB(RarityStrong(definition.rarity))}>" +
                                $"{CardDefinition.RarityName(definition.rarity).ToUpperInvariant()}</color>";
            switch (definition)
            {
                case MonsterCardData monster:
                    string attrHex = ColorUtility.ToHtmlStringRGB(MonsterCardData.AttributeColor(monster.attribute));
                    string reliquaryTag = definition is ReliquaryCardData ? "<color=#F1E7D2>RELIQUARY</color> · " : "";
                    return reliquaryTag + $"<color=#{attrHex}>{monster.attribute.ToString().ToUpperInvariant()}</color>" +
                           $" / {monster.monsterType.ToString().ToUpperInvariant()} · {monster.atk} / {monster.def} · {rarityWord}";
                case SpellCardData spell:
                    return (spell.speed == SpellSpeed.Quick ? "QUICK SPELL" : "SPELL") + $" · {rarityWord}";
                case ArtifactCardData artifact:
                    return $"ARTIFACT / {TcgCardView.ArtifactSlotName(artifact.slot).ToUpperInvariant()} · {rarityWord}";
                default:
                    return rarityWord;
            }
        }

        private static void StyleControl(Button button, Image bg, Image frameImg, TMP_Text label, bool enabled, bool isRemove)
        {
            if (button != null) button.interactable = enabled;
            if (bg != null)
                bg.color = !enabled ? new Color(0f, 0f, 0f, 0.3f)
                    : isRemove ? new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.15f)
                    : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.18f);
            if (frameImg != null)
                frameImg.color = !enabled ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.15f)
                    : isRemove ? new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.5f)
                    : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.55f);
            if (label != null)
                label.color = !enabled ? FromHex("#4A4235") : isRemove ? FromHex("#E9A183") : FromHex("#F3DDA4");
        }

        /// <summary>Ausgewählt = Karte, die gerade in der Detail-Rail steht.</summary>
        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            ApplyRowColors();
        }

        /// <summary>Auswahl leuchtet kräftig, bloßes Überfahren nur angedeutet.</summary>
        private void ApplyRowColors()
        {
            if (background != null)
                background.color = selected
                    ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.14f)
                    : hovered
                        ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.06f)
                        : new Color(0f, 0f, 0f, 0.38f);
            if (frame != null)
            {
                var keyline = TypeKeyline(card);
                frame.color = selected
                    ? keyline
                    : hovered
                        ? new Color(keyline.r, keyline.g, keyline.b, 0.45f)
                        : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.18f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            ApplyRowColors();
            SfxManager.Hover(SfxManager.ButtonHoverGain);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            ApplyRowColors();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // −/+ stoppen die Propagation über eigene Buttons; hier zählt nur der Zeilenkörper
            SfxManager.Click();
            if (eventData.clickCount >= 2)
            {
                // Doppelklick betrifft genau das Exemplar dieser Zeile
                if (deckSide) onRemove?.Invoke(card, finish);
                else onAdd?.Invoke(card, finish);
                return;
            }
            // Die Vorschau zeigt GENAU dieses Exemplar — wer die Static-Zeile
            // anklickt, will die Static-Karte sehen und nicht die schlichte.
            onSelect?.Invoke(card, finish);
        }

        // ================== HERAUSZIEHEN ==================

        /// <summary>
        /// Aktiviert das Herausziehen: wird die Zeile außerhalb dieses Rechtecks
        /// (des Deck-Panels) losgelassen, fliegt das Exemplar aus dem Deck.
        /// Ohne diesen Aufruf bleibt die Zeile unziehbar — die Listen im Shop
        /// und im Pack-Reveal wollen davon nichts wissen.
        /// </summary>
        public void SetDragArea(RectTransform area) => dragArea = area;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!deckSide || dragArea == null || card == null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Der Geist ist eine Kopie der Zeile selbst — inert, ohne Raycasts
            var size = ((RectTransform)transform).rect.size;
            dragGhost = Instantiate(gameObject, canvas.rootCanvas.transform);
            dragGhost.name = "RowDragGhost";
            var ghostRect = (RectTransform)dragGhost.transform;
            ghostRect.anchorMin = ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            ghostRect.sizeDelta = size;
            ghostRect.localScale = Vector3.one;
            dragGhostGroup = dragGhost.GetComponent<CanvasGroup>();
            if (dragGhostGroup == null) dragGhostGroup = dragGhost.AddComponent<CanvasGroup>();
            dragGhostGroup.blocksRaycasts = false;
            dragGhostGroup.alpha = 0.85f;
            MoveGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData) => MoveGhost(eventData);

        private void MoveGhost(PointerEventData eventData)
        {
            if (dragGhost == null) return;
            var ghostRect = (RectTransform)dragGhost.transform;
            var canvasRect = (RectTransform)ghostRect.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out var local);
            ghostRect.anchoredPosition = local;
            // Außerhalb des Decks färbt sich der Geist rot an — gleich fliegt die Karte raus
            bool outside = !IsInsideDeck(eventData);
            dragGhostGroup.alpha = outside ? 1f : 0.7f;
            var ghostBg = dragGhost.GetComponent<Image>();
            if (ghostBg != null)
                ghostBg.color = outside
                    ? new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.35f)
                    : new Color(0f, 0f, 0f, 0.38f);
        }

        private bool IsInsideDeck(PointerEventData eventData) =>
            dragArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(dragArea, eventData.position, eventData.pressEventCamera);

        public void OnEndDrag(PointerEventData eventData)
        {
            bool removed = dragGhost != null && !IsInsideDeck(eventData);
            if (dragGhost != null) Destroy(dragGhost);
            dragGhost = null;
            dragGhostGroup = null;
            if (removed)
            {
                SfxManager.Click();
                onSelect?.Invoke(card, finish);
                onRemove?.Invoke(card, finish);
            }
        }
    }
}
