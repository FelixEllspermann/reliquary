using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Macht Kartennamen in einem TMP-Text hoverbar und färbt sie nach Kartentyp:
    /// Namen werden per Linkify() mit &lt;link&gt;- und &lt;color&gt;-Tags umschlossen;
    /// beim Hovern zeigt das Detail-Panel die Karte.
    /// </summary>
    public class CardLinkText : MonoBehaviour, IPointerMoveHandler
    {
        // Reihenfolge: Monster, Spell, Artifact, Reliquary, Held
        private const int KindMonster = 0, KindSpell = 1, KindArtifact = 2, KindReliquary = 3, KindHero = 4;

        /// <summary>Helle Tinten für dunkle Flächen — Prompt-Fenster und Statuszeile.</summary>
        private static readonly string[] InkOnDark = { "E5BC72", "8FC6D2", "B9A3E0", "F1E7D2", "F3DDA4" };

        /// <summary>Dunkle Tinten für helle Flächen — das Pergament des Duell-Logs.</summary>
        private static readonly string[] InkOnLight = { "8A5512", "15697C", "5B3A9E", "A8452C", "6E4E0E" };

        private static CardDetailPanel panel;
        private static CardCatalog catalog;

        /// <summary>Kartennamen nach erstem Buchstaben gebündelt, je Bündel längste zuerst.</summary>
        private static Dictionary<char, List<string>> namesByFirstChar;
        private static Dictionary<string, int> kindByName;

        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        /// <summary>Einmal pro Duell-Szene aufrufen (z.B. vom Board-Renderer).</summary>
        public static void Configure(CardDetailPanel detailPanel, CardCatalog cardCatalog)
        {
            panel = detailPanel;
            catalog = cardCatalog;
            namesByFirstChar = null;
            kindByName = null;
        }

        private static int KindOf(CardDefinition definition)
        {
            if (definition is ReliquaryCardData) return KindReliquary;
            if (definition is PlayerCardData) return KindHero;
            if (definition is SpellCardData) return KindSpell;
            if (definition is ArtifactCardData) return KindArtifact;
            return KindMonster;
        }

        /// <summary>Farbe eines Kartentyps als Hex, passend zum Untergrund.</summary>
        public static string InkFor(CardDefinition definition, bool onLightBackground = false)
        {
            return (onLightBackground ? InkOnLight : InkOnDark)[KindOf(definition)];
        }

        /// <summary>Macht einen TMP-Text hoverbar (Komponente + Raycast aktivieren).</summary>
        public static void Attach(TMP_Text target)
        {
            if (target == null) return;
            target.raycastTarget = true;
            if (target.GetComponent<CardLinkText>() == null)
                target.gameObject.AddComponent<CardLinkText>();
        }

        /// <summary>
        /// Umschließt jeden bekannten Kartennamen mit &lt;link&gt;- und Typfarbe. Läuft
        /// einmal von links nach rechts und überspringt gefundene Namen: dadurch kann
        /// ein kurzer Name nicht mehr innerhalb eines bereits ersetzten längeren Namens
        /// noch einmal zuschlagen.
        /// </summary>
        public static string Linkify(string message, bool onLightBackground = false)
        {
            if (string.IsNullOrEmpty(message) || catalog == null || message.Contains("<link=")) return message;
            BuildIndex();

            var inks = onLightBackground ? InkOnLight : InkOnDark;
            var builder = new StringBuilder(message.Length + 64);
            int i = 0;
            while (i < message.Length)
            {
                string hit = MatchAt(message, i);
                if (hit == null)
                {
                    builder.Append(message[i]);
                    i++;
                    continue;
                }
                builder.Append("<link=\"").Append(hit).Append("\"><color=#").Append(inks[kindByName[hit]]).Append(">")
                       .Append(hit).Append("</color></link>");
                i += hit.Length;
            }
            return builder.ToString();
        }

        /// <summary>Längster Kartenname, der genau an dieser Stelle beginnt (oder null).</summary>
        private static string MatchAt(string message, int index)
        {
            if (!namesByFirstChar.TryGetValue(message[index], out var candidates)) return null;
            foreach (var name in candidates)   // längste zuerst
            {
                if (name.Length > message.Length - index) continue;
                if (string.CompareOrdinal(message, index, name, 0, name.Length) == 0) return name;
            }
            return null;
        }

        private static void BuildIndex()
        {
            if (namesByFirstChar != null) return;
            namesByFirstChar = new Dictionary<char, List<string>>();
            kindByName = new Dictionary<string, int>();

            foreach (var card in catalog.cards)
            {
                if (card == null || string.IsNullOrEmpty(card.cardName)) continue;
                if (kindByName.ContainsKey(card.cardName)) continue;
                kindByName[card.cardName] = KindOf(card);

                char key = card.cardName[0];
                if (!namesByFirstChar.TryGetValue(key, out var bucket))
                {
                    bucket = new List<string>();
                    namesByFirstChar[key] = bucket;
                }
                bucket.Add(card.cardName);
            }
            foreach (var bucket in namesByFirstChar.Values)
                bucket.Sort((a, b) => b.Length.CompareTo(a.Length));
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (panel == null || catalog == null || text == null) return;
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.enterEventCamera);
            if (linkIndex < 0) return;
            var definition = catalog.FindByName(text.textInfo.linkInfo[linkIndex].GetLinkID());
            if (definition != null) panel.ShowDefinition(definition);
        }
    }
}
