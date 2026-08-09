using System;
using Rouge.Tcg.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Eine Karten-Kachel des Deck Builders (Pool UND Deck-Liste): das Kartenbild
    /// als Kompakt-Rendition (unter 200 px Breite schaltet TcgCardView von selbst
    /// um) statt der alten Text-Zeile. Overlays erzählen den Rest: Banlist-Marke
    /// oben links, „im Deck"-Abzeichen oben rechts, Rarity-Raute und Besitz in der
    /// Zähl-Leiste darunter, Dunkelschleier über allem, was man nicht besitzt.
    ///
    /// Bedienung wie die alten Zeilen: Klick wählt für die Detail-Rail,
    /// Doppelklick legt ins Deck (Pool) bzw. nimmt heraus (Deck-Seite), dazu −/+
    /// in der Leiste — und die Kachel lässt sich als Geisterkarte ZIEHEN:
    /// Pool-Kacheln ins Deck-Panel (Drop = einbauen), Deck-Kacheln aus dem
    /// Deck-Panel heraus (der Geist färbt sich rot, Drop = entfernen).
    /// Gebaut wird die Kachel EINMAL (Build), danach nur noch befüllt (Setup) —
    /// beide Listen recyceln ihre Kacheln bei jedem Rebuild, statt sie neu zu erzeugen.
    /// </summary>
    public class CollectionCardTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Zellmaße für das GridLayout des Pools (4 Spalten im 581er-Viewport).</summary>
        public const float Width = 130f;
        public const float Height = 210f;

        private const float CardHeight = 182f;
        private const float BarHeight = 26f;

        private TcgCardView view;
        private CanvasGroup viewGroup;
        private GameObject lockOverlay;
        private Image selectFrame;
        private Image rarityGem;
        private GameObject banChip;
        private TMP_Text banText;
        private GameObject deckBadge;
        private Image deckBadgeBg;
        private Image deckBadgeFrame;
        private TMP_Text deckBadgeText;
        private TMP_Text countLabel;
        private Button minusButton;
        private Image minusBg;
        private TMP_Text minusText;
        private Button plusButton;
        private Image plusBg;
        private TMP_Text plusText;

        private CardDefinition card;
        private CardFinish finish;
        private bool deckSide;
        private Action<CardDefinition, CardFinish> onAdd;
        private Action<CardDefinition, CardFinish> onRemove;
        private Action<CardDefinition, CardFinish> onSelect;
        private Action<CardDefinition, CardFinish> onInspect;
        private bool selected;
        private bool hovered;

        // ---- Drag & Drop: Pool-Kachel INS Deck-Panel, Deck-Kachel HERAUS ----
        private RectTransform dropTarget;
        private TcgCardView dragGhost;
        private CanvasGroup dragGhostGroup;
        private Image dragGhostTint;   // roter Schleier: gleich fliegt die Karte raus

        public CardDefinition Card => card;

        /// <summary>Die Ausführung, für die diese Kachel steht.</summary>
        public CardFinish Finish => finish;

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        /// <summary>
        /// Baut die Kinder der Kachel — einmalig. Das Kartenbild ist ein Klon der
        /// Vorschaukarte der Detail-Rail; bei 134 px Breite rendert TcgCardView
        /// von selbst die Kompakt-Ansicht.
        /// </summary>
        public void Build(TcgCardView template, CardSkin skin)
        {
            if (view != null || template == null) return;

            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(Width, Height);

            view = Instantiate(template, transform);
            view.name = "Card";
            view.HoverLift = false;
            // Die Vorschau trägt zur Laufzeit einen Finish-Umschalter als Kind —
            // ein mitgeklontes Exemplar hätte hier nichts verloren.
            var strayStrip = view.transform.Find("FinishStrip");
            if (strayStrip != null) Destroy(strayStrip.gameObject);
            var viewRect = (RectTransform)view.transform;
            viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 1f);
            viewRect.pivot = new Vector2(0.5f, 1f);
            viewRect.anchoredPosition = Vector2.zero;
            viewRect.sizeDelta = new Vector2(Width, CardHeight);
            viewRect.localScale = Vector3.one;
            viewGroup = view.GetComponent<CanvasGroup>();
            if (viewGroup == null) viewGroup = view.gameObject.AddComponent<CanvasGroup>();

            var fontSource = view.GetComponentInChildren<TMP_Text>(true);

            // Dunkelschleier über Karten, die man nicht besitzt
            var lockRect = MakeRect(transform, "LockOverlay");
            SetCardArea(lockRect, 0f);
            var lockImage = lockRect.gameObject.AddComponent<Image>();
            lockImage.color = new Color(0.02f, 0.02f, 0.04f, 0.5f);
            lockImage.raycastTarget = false;
            lockOverlay = lockRect.gameObject;

            // Banlist-Marke links auf der Karte — UNTER der Namenszeile der
            // Kompakt-Rendition, sonst überdeckt sie den Kartennamen
            var banRect = MakeRect(transform, "BanChip");
            banRect.anchorMin = banRect.anchorMax = new Vector2(0f, 1f);
            banRect.pivot = new Vector2(0f, 1f);
            banRect.anchoredPosition = new Vector2(4f, -26f);
            banRect.sizeDelta = new Vector2(34f, 20f);
            var banBg = banRect.gameObject.AddComponent<Image>();
            banBg.color = new Color(0f, 0f, 0f, 0.78f);
            banBg.raycastTarget = false;
            banText = MakeText(banRect, "Label", 12f, TextAlignmentOptions.Center, fontSource);
            banChip = banRect.gameObject;

            // „Im Deck"-Abzeichen rechts auf der Karte — ebenfalls unter der
            // Namenszeile, dort sitzt sonst schon das Level-Wappen. Dunkles
            // Plättchen mit Gold-Keyline statt heller Fläche: auf goldenen
            // Artworks (Münzen, Licht) ging die helle Variante einfach unter.
            var badgeRect = MakeRect(transform, "DeckBadge");
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-3f, -25f);
            badgeRect.sizeDelta = new Vector2(46f, 28f);
            deckBadgeBg = badgeRect.gameObject.AddComponent<Image>();
            deckBadgeBg.color = new Color(0.05f, 0.04f, 0.02f, 0.92f);
            deckBadgeBg.raycastTarget = false;
            var badgeFrameRect = MakeRect(badgeRect, "Frame");
            badgeFrameRect.anchorMin = Vector2.zero;
            badgeFrameRect.anchorMax = Vector2.one;
            badgeFrameRect.offsetMin = Vector2.zero;
            badgeFrameRect.offsetMax = Vector2.zero;
            deckBadgeFrame = badgeFrameRect.gameObject.AddComponent<Image>();
            if (skin != null && skin.whiteFrame != null)
            {
                deckBadgeFrame.sprite = skin.whiteFrame;
                deckBadgeFrame.type = Image.Type.Sliced;
            }
            deckBadgeFrame.raycastTarget = false;
            deckBadgeText = MakeText(badgeRect, "Label", 17f, TextAlignmentOptions.Center, fontSource);
            deckBadgeText.fontStyle = FontStyles.Bold;
            deckBadge = badgeRect.gameObject;

            // Auswahl-/Hover-Rahmen um das Kartenbild
            var frameRect = MakeRect(transform, "SelectFrame");
            SetCardArea(frameRect, 3f);
            selectFrame = frameRect.gameObject.AddComponent<Image>();
            selectFrame.raycastTarget = false;
            selectFrame.color = Color.clear;
            if (skin != null && skin.whiteFrame != null)
            {
                selectFrame.sprite = skin.whiteFrame;
                selectFrame.type = Image.Type.Sliced;
            }
            else selectFrame.enabled = false;

            // Zähl-Leiste unter dem Kartenbild
            var barRect = MakeRect(transform, "CountBar");
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, BarHeight);
            var barBg = barRect.gameObject.AddComponent<Image>();
            barBg.color = new Color(0f, 0f, 0f, 0.55f);

            minusButton = MakeBarButton(barRect, "-", true, fontSource, out minusBg, out minusText);
            plusButton = MakeBarButton(barRect, "+", false, fontSource, out plusBg, out plusText);

            var gemRect = MakeRect(barRect, "RarityGem");
            gemRect.anchorMin = gemRect.anchorMax = new Vector2(0f, 0.5f);
            gemRect.pivot = new Vector2(0.5f, 0.5f);
            gemRect.anchoredPosition = new Vector2(35f, 0f);
            gemRect.sizeDelta = new Vector2(9f, 9f);
            gemRect.localEulerAngles = new Vector3(0f, 0f, 45f);
            rarityGem = gemRect.gameObject.AddComponent<Image>();
            rarityGem.raycastTarget = false;

            countLabel = MakeText(barRect, "Count", 11.5f, TextAlignmentOptions.Center, fontSource);
            var countRect = (RectTransform)countLabel.transform;
            countRect.offsetMin = new Vector2(44f, 0f);
            countRect.offsetMax = new Vector2(-30f, 0f);
            countLabel.enableAutoSizing = true;
            countLabel.fontSizeMin = 8f;
            countLabel.fontSizeMax = 12f;
            // "NOT OWNED" darf nicht zweizeilig umbrechen — lieber kleiner werden
            countLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // Klick-Fänger über dem Kartenbild: fängt die Raycasts vor dem
            // TcgCardView ab (das eigene Pointer-Handler trägt) und lässt sie
            // zum Kachel-Handler hochsteigen. Die Leiste braucht ihn nicht —
            // ihr Hintergrund ist selbst Raycast-Ziel.
            var catcherRect = MakeRect(transform, "ClickCatcher");
            SetCardArea(catcherRect, 0f);
            var catcher = catcherRect.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
        }

        /// <summary>Rechteck deckungsgleich mit dem Kartenbild (plus Rand nach außen).</summary>
        private static void SetCardArea(RectTransform rect, float grow)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, grow);
            rect.sizeDelta = new Vector2(Width + grow * 2f, CardHeight + grow * 2f);
        }

        private static RectTransform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text MakeText(Transform parent, string name, float size, TextAlignmentOptions align, TMP_Text fontSource)
        {
            var rect = MakeRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (fontSource != null)
            {
                text.font = fontSource.font;
                text.fontSharedMaterial = fontSource.fontSharedMaterial;
            }
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeBarButton(Transform bar, string label, bool leftSide, TMP_Text fontSource, out Image bg, out TMP_Text text)
        {
            var rect = MakeRect(bar, leftSide ? "Minus" : "Plus");
            rect.anchorMin = new Vector2(leftSide ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(leftSide ? 0f : 1f, 1f);
            rect.pivot = new Vector2(leftSide ? 0f : 1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(26f, 0f);
            bg = rect.gameObject.AddComponent<Image>();
            text = MakeText(rect, "Label", 15f, TextAlignmentOptions.Center, fontSource);
            text.text = label;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        /// <summary>
        /// Befüllt die Kachel neu — dieselbe Zähl-Logik wie CollectionRow.Setup:
        /// <paramref name="maxCopies"/> und <paramref name="copiesOfCard"/> zählen
        /// ALLE Finishes der Karte zusammen (Banlist), <paramref name="owned"/>
        /// und <paramref name="inDeck"/> nur dieses eine Finish.
        /// </summary>
        public void Setup(CardDefinition definition, CardFinish cardFinish, int inDeck, int owned,
            int maxCopies, int copiesOfCard,
            Action<CardDefinition, CardFinish> add, Action<CardDefinition, CardFinish> remove,
            Action<CardDefinition, CardFinish> select, int banLimit, bool collectionMode,
            bool isDeckSide = false)
        {
            card = definition;
            finish = cardFinish;
            deckSide = isDeckSide;
            onAdd = add;
            onRemove = remove;
            onSelect = select;
            if (view == null || definition == null) return;

            view.Show(new CardInstance(definition, null) { Finish = cardFinish }, false, upright: true);

            // Nicht besessene Karten stehen gedimmt und verschleiert da — man
            // sieht, was es gibt, und auf einen Blick, dass es noch fehlt.
            bool missing = collectionMode && owned <= 0;
            if (viewGroup != null) viewGroup.alpha = missing ? 0.45f : 1f;
            if (lockOverlay != null) lockOverlay.SetActive(missing);

            if (rarityGem != null) rarityGem.color = CollectionRow.RarityStrong(definition.rarity);

            if (banChip != null) banChip.SetActive(banLimit >= 0);
            if (banText != null && banLimit >= 0)
                banText.text = $"<color=#{CollectionRow.RestrictionHex(banLimit)}><b>[{banLimit}]</b></color>";

            bool overLimit = banLimit >= 0 && copiesOfCard > banLimit;
            if (deckBadge != null) deckBadge.SetActive(inDeck > 0);
            if (inDeck > 0)
            {
                // Zu viele Kopien (etwa nach einer neuen Banlist) schlagen rot an
                if (deckBadgeText != null)
                {
                    deckBadgeText.text = "×" + inDeck;
                    deckBadgeText.color = overLimit ? Hex("#E0603A") : Hex("#F3DDA4");
                }
                if (deckBadgeFrame != null)
                    deckBadgeFrame.color = overLimit
                        ? Hex("#E0603A")
                        : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.9f);
            }

            if (countLabel != null)
            {
                if (!collectionMode)
                    countLabel.text = "";   // Sandbox: alles frei, eine Stückzahl wäre gelogen
                else if (cardFinish != CardFinish.Plain)
                {
                    countLabel.text = $"{CardFinishInfo.Glyph(cardFinish)} ×{owned}";
                    countLabel.color = CardFinishInfo.Accent(cardFinish);
                }
                else if (owned <= 0)
                {
                    countLabel.text = "NOT OWNED";
                    countLabel.color = Hex("#A66A50");
                }
                else
                {
                    countLabel.text = $"×{owned} OWNED";
                    // Volles Playset leuchtet gold — daran erkennt man, was fertig gesammelt ist
                    countLabel.color = owned >= maxCopies ? Hex("#F3DDA4") : Hex("#CFC3AC");
                }
            }

            bool canAdd = inDeck < owned && copiesOfCard < maxCopies;
            bool canRemove = inDeck > 0;
            StyleBarButton(minusButton, minusBg, minusText, canRemove, true);
            StyleBarButton(plusButton, plusBg, plusText, canAdd, false);
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
            ApplyFrame();
        }

        /// <summary>Farben wie die −/+ Knöpfe der alten Zeilen.</summary>
        private static void StyleBarButton(Button button, Image bg, TMP_Text label, bool enabled, bool isRemove)
        {
            if (button != null) button.interactable = enabled;
            if (bg != null)
                bg.color = !enabled ? new Color(0f, 0f, 0f, 0.3f)
                    : isRemove ? new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.2f)
                    : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.22f);
            if (label != null)
                label.color = !enabled ? Hex("#4A4235") : isRemove ? Hex("#E9A183") : Hex("#F3DDA4");
        }

        /// <summary>Ausgewählt = Karte, die gerade in der Detail-Rail steht.</summary>
        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (selectFrame == null) return;
            if (selected)
                selectFrame.color = new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.95f);
            else if (hovered && card != null)
            {
                var keyline = CollectionRow.TypeKeyline(card);
                selectFrame.color = new Color(keyline.r, keyline.g, keyline.b, 0.5f);
            }
            else selectFrame.color = Color.clear;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            ApplyFrame();
            SfxManager.Hover(SfxManager.ButtonHoverGain);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            ApplyFrame();
        }

        /// <summary>
        /// Trennt „Karte ansehen" von „Auswahl folgt der Aktion": ist ein
        /// Inspect-Handler gesetzt, feuert er NUR beim einfachen Klick auf die
        /// Kachel — nicht bei −/+ und nicht am Ende eines Drags. Der Draft-
        /// Builder hängt hier sein großes Karten-Overlay an; ohne diese
        /// Trennung ploppte es bei jedem Knopfdruck und jedem Ziehen auf.
        /// </summary>
        public void SetInspect(Action<CardDefinition, CardFinish> handler) => onInspect = handler;

        public void OnPointerClick(PointerEventData eventData)
        {
            // −/+ verschlucken ihre Klicks selbst; hier landen Kartenbild und Leiste
            SfxManager.Click();
            if (eventData.clickCount >= 2)
            {
                // Doppelklick betrifft genau das Exemplar dieser Kachel
                if (deckSide) onRemove?.Invoke(card, finish);
                else onAdd?.Invoke(card, finish);
                return;
            }
            if (onInspect != null) onInspect(card, finish);
            else onSelect?.Invoke(card, finish);
        }

        // ================== DRAG & DROP ==================

        /// <summary>
        /// Das Deck-Panel — vom Controller gesetzt. Für Pool-Kacheln ist es das
        /// Drop-ZIEL (hineinziehen = einbauen), für Deck-Kacheln die GRENZE
        /// (hinausziehen = entfernen).
        /// </summary>
        public void SetDropTarget(RectTransform target) => dropTarget = target;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (card == null || view == null || dropTarget == null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Geisterkarte am Zeiger: ein Klon des Kartenbilds in Handkarten-Größe.
            // blocksRaycasts aus, sonst fängt der Geist selbst den Zeiger und das
            // Ziel unter ihm wird nie getroffen.
            dragGhost = Instantiate(view, canvas.rootCanvas.transform);
            dragGhost.name = "DragGhost";
            var ghostRect = (RectTransform)dragGhost.transform;
            ghostRect.anchorMin = ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            ghostRect.sizeDelta = new Vector2(112f, 157f);
            ghostRect.localScale = Vector3.one;
            dragGhostGroup = dragGhost.GetComponent<CanvasGroup>();
            if (dragGhostGroup == null) dragGhostGroup = dragGhost.gameObject.AddComponent<CanvasGroup>();
            dragGhostGroup.blocksRaycasts = false;
            dragGhostGroup.alpha = 0.85f;

            // Roter Schleier für die Deck-Seite: sichtbar, sobald der Drop entfernt
            var tintRect = MakeRect(dragGhost.transform, "RemoveTint");
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            dragGhostTint = tintRect.gameObject.AddComponent<Image>();
            dragGhostTint.color = new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.35f);
            dragGhostTint.raycastTarget = false;
            dragGhostTint.gameObject.SetActive(false);

            SfxManager.Hover(SfxManager.ButtonHoverGain);
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
            // Wo der Drop WIRKT, wird der Geist satt: für Pool-Kacheln über dem
            // Deck, für Deck-Kacheln außerhalb davon — dort zusätzlich rot.
            bool effective = DropEffective(eventData);
            dragGhostGroup.alpha = effective ? 1f : 0.6f;
            if (dragGhostTint != null) dragGhostTint.gameObject.SetActive(deckSide && effective);
        }

        private bool IsOverDrop(PointerEventData eventData) =>
            dropTarget != null &&
            RectTransformUtility.RectangleContainsScreenPoint(dropTarget, eventData.position, eventData.pressEventCamera);

        /// <summary>Würde Loslassen an dieser Zeigerposition etwas bewirken?</summary>
        private bool DropEffective(PointerEventData eventData) =>
            deckSide ? !IsOverDrop(eventData) : IsOverDrop(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            bool effective = dragGhost != null && DropEffective(eventData);
            if (dragGhost != null) Destroy(dragGhost.gameObject);
            dragGhost = null;
            dragGhostGroup = null;
            dragGhostTint = null;
            if (effective)
            {
                SfxManager.Click();
                onSelect?.Invoke(card, finish);
                if (deckSide) onRemove?.Invoke(card, finish);
                else onAdd?.Invoke(card, finish);
            }
        }
    }
}
