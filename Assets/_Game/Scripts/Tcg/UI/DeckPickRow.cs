using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Eine Deck-Karte im Duel-Setup-Picker: Rücken-Thumbnail mit Deck-Gem, Name,
    /// Legalitäts-Badge, Meta-Zeile, Count-Bar und Attribut-Spread plus Auswahl-Kreis.
    /// Illegale Decks werden gedimmt, bleiben aber wählbar.
    /// </summary>
    public class DeckPickRow : MonoBehaviour, IPointerClickHandler
    {
        [Header("Referenzen (vom Builder verdrahtet)")]
        [SerializeField] private Image background;
        [SerializeField] private Image frame;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image gem;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image badgeBg;
        [SerializeField] private Image badgeFrame;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private Image countFill;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Transform spreadBar;
        [SerializeField] private Image selectCircle;
        [SerializeField] private Image selectFrame;
        [SerializeField] private TMP_Text selectMark;

        private Action onClicked;
        private static readonly Color[] GemColors =
        {
            new Color32(0xEB, 0xCE, 0x8A, 0xFF), new Color32(0x8F, 0xC6, 0xD2, 0xFF),
            new Color32(0xB9, 0xA3, 0xE0, 0xFF), new Color32(0xE0, 0xA0, 0x7A, 0xFF),
            new Color32(0x9F, 0xDC, 0xBE, 0xFF), new Color32(0xE0, 0x60, 0x3A, 0xFF)
        };

        public void Setup(Net.RuntimeDeck deck, int index, bool legal, string legalLabel,
            CardCatalog catalog, Action clicked)
        {
            onClicked = clicked;
            if (gem != null) gem.color = GemColors[index % GemColors.Length];
            if (nameText != null) nameText.text = deck.Name;

            if (badgeText != null) badgeText.text = legalLabel;
            Color good = new Color32(0x7A, 0xCD, 0x96, 0xFF);
            Color bad = new Color32(0xE9, 0xA1, 0x83, 0xFF);
            if (badgeBg != null)
                badgeBg.color = legal ? new Color(122f / 255f, 205f / 255f, 150f / 255f, 0.14f)
                    : new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.14f);
            if (badgeFrame != null)
                badgeFrame.color = legal ? new Color(122f / 255f, 205f / 255f, 150f / 255f, 0.5f)
                    : new Color(224f / 255f, 96f / 255f, 58f / 255f, 0.5f);
            if (badgeText != null) badgeText.color = legal ? good : bad;

            // Meta + Spread aus dem Katalog ableiten
            int monsters = 0, spells = 0, artifacts = 0;
            var attrCounts = new Dictionary<MonsterAttribute, int>();
            foreach (var cardName in deck.Cards)
            {
                var definition = catalog != null ? catalog.FindByName(cardName) : null;
                if (definition is MonsterCardData m)
                {
                    monsters++;
                    attrCounts[m.attribute] = attrCounts.TryGetValue(m.attribute, out int a) ? a + 1 : 1;
                }
                else if (definition is SpellCardData) spells++;
                else if (definition is ArtifactCardData) artifacts++;
            }
            string heroName = string.IsNullOrEmpty(deck.Hero) ? "—" : deck.Hero.Split(' ')[0];
            if (metaText != null)
                metaText.text = $"Hero: {heroName} · {monsters} Monster · {spells} Spell · {artifacts} Artifact";

            if (countFill != null)
            {
                countFill.fillAmount = Mathf.Clamp01(deck.Cards.Count / 80f);
                countFill.color = legal ? new Color32(0xF3, 0xDD, 0xA4, 0xE6) : new Color32(0xE9, 0xA1, 0x83, 0xE6);
            }
            if (countText != null)
            {
                countText.text = deck.Cards.Count.ToString();
                countText.color = legal ? good : bad;
            }

            if (spreadBar != null)
            {
                int total = 0;
                foreach (var kv in attrCounts) total += kv.Value;
                foreach (Transform segment in spreadBar)
                {
                    var image = segment.GetComponent<Image>();
                    var layout = segment.GetComponent<LayoutElement>();
                    if (image == null || layout == null) continue;
                    if (Enum.TryParse(segment.name, out MonsterAttribute attr) && attrCounts.TryGetValue(attr, out int n) && total > 0)
                    {
                        segment.gameObject.SetActive(true);
                        layout.flexibleWidth = n;
                        image.color = MonsterCardData.AttributeColor(attr);
                    }
                    else segment.gameObject.SetActive(false);
                }
            }

            if (group != null) group.alpha = legal ? 1f : 0.62f;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected
                    ? new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.13f)
                    : new Color(0f, 0f, 0f, 0.38f);
            if (frame != null)
                frame.color = selected
                    ? new Color32(0xC8, 0xA4, 0x5C, 0xFF)
                    : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.2f);
            if (selectCircle != null)
                selectCircle.color = selected ? new Color32(0xE2, 0xC6, 0x85, 0xFF) : new Color(0f, 0f, 0f, 0.4f);
            if (selectFrame != null)
                selectFrame.color = selected
                    ? new Color32(0xEB, 0xCE, 0x8A, 0xFF)
                    : new Color(200f / 255f, 164f / 255f, 92f / 255f, 0.25f);
            if (selectMark != null) selectMark.gameObject.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData) => onClicked?.Invoke();
    }
}
