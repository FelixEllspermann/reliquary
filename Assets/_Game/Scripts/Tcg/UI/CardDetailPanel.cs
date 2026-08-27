using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>Großes Detail-Panel links: voller Kartentext beim Hovern/Auswählen.</summary>
    public class CardDetailPanel : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private Image frameImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text rulesText;
        [SerializeField] private Image artworkImage;

        [Header("Duel-Field-Modus (optional): echte Karte statt Textblock")]
        [SerializeField] private TcgCardView cardView;   // zeigt die Karte im Reliquary-Design
        [SerializeField] private GameObject backPlate;   // Kartenrücken für leer/verdeckt
        [SerializeField] private GameObject caption;     // "SELECT A CARD"-Hinweis
        [SerializeField] private TMP_Text rarityChipText;
        [SerializeField] private Image rarityChipBg;
        [SerializeField] private ScrollRect rulesScroll; // langer Regeltext scrollt (Mausrad)

        private bool UseCardView => cardView != null;

        private float rulesTopWithoutArt = float.NaN;

        /// <summary>Zeigt/versteckt das Artwork und rückt den Regeltext entsprechend nach oben/unten.</summary>
        private void ApplyArtwork(Sprite sprite)
        {
            if (artworkImage == null) return;
            artworkImage.enabled = sprite != null;
            artworkImage.sprite = sprite;

            if (rulesText == null) return;
            var rulesRect = (RectTransform)rulesText.transform;
            if (float.IsNaN(rulesTopWithoutArt)) rulesTopWithoutArt = rulesRect.offsetMax.y;
            var artRect = (RectTransform)artworkImage.transform;
            float top = sprite != null
                ? artRect.anchoredPosition.y - artRect.sizeDelta.y - 10f
                : rulesTopWithoutArt;
            rulesRect.offsetMax = new Vector2(rulesRect.offsetMax.x, top);
        }

        public void ShowCard(CardInstance instance)
        {
            if (instance == null || instance.Definition == null) return;
            if (UseCardView) { ShowOnCardView(instance); return; }
            ShowDefinition(instance.Definition, instance);
        }

        /// <summary>Duel-Field-Modus: rendert die Karte selbst im Viewport statt Textfeldern.</summary>
        private void ShowOnCardView(CardInstance instance)
        {
            if (backPlate != null) backPlate.SetActive(false);
            if (caption != null) caption.SetActive(false);
            cardView.gameObject.SetActive(true);
            cardView.BadgeTooltips = true; // Hover auf den Status-Badges erklärt den Status
            cardView.Show(instance, false, upright: true, revealFaceDown: true);
            cardView.SetHighlight(false);

            var definition = instance.Definition;
            if (rarityChipText != null)
            {
                rarityChipText.text = Loc.T(CardDefinition.RarityName(definition.rarity).ToUpperInvariant());
                rarityChipText.color = CardDefinition.RarityColor(definition.rarity);
            }
            if (rarityChipBg != null) rarityChipBg.gameObject.SetActive(true);
            SetInspectRules(BuildRulesBody(definition, instance));
        }

        /// <summary>Setzt den großen Lesetext unter der Inspect-Karte (Panel nur bei Inhalt zeigen).</summary>
        private void SetInspectRules(string body)
        {
            if (rulesText == null) return;
            rulesText.text = body;
            var host = rulesScroll != null ? rulesScroll.gameObject
                : rulesText.transform.parent != null ? rulesText.transform.parent.gameObject : null;
            if (host != null && host != gameObject) host.SetActive(!string.IsNullOrEmpty(body));
            rulesText.gameObject.SetActive(!string.IsNullOrEmpty(body));
            if (rulesScroll != null) rulesScroll.verticalNormalizedPosition = 1f; // bei Kartenwechsel nach oben
        }

        /// <summary>Formatierter Regeltext inkl. Liste ausgerüsteter Artefakte.</summary>
        private static string BuildRulesBody(CardDefinition definition, CardInstance instance)
        {
            string text = BuildFormattedRulesText(definition);
            if (instance != null && instance.EquippedArtifacts.Count > 0)
            {
                var equipInfo = new System.Text.StringBuilder(text);
                equipInfo.Append($"\n\n<b>{Loc.T("Equipped:")}</b>");
                foreach (var equipped in instance.EquippedArtifacts)
                {
                    equipInfo.Append($"\n• {Loc.CardName(equipped.Name)}");
                    var data = equipped.ArtifactData;
                    if (data != null && (data.atkBonus != 0 || data.defBonus != 0))
                        equipInfo.Append($" (+{data.atkBonus} ATK / +{data.defBonus} DEF)");
                }
                text = equipInfo.ToString();
            }
            return text;
        }

        public void ShowDefinition(CardDefinition definition, CardInstance instance = null)
        {
            if (definition == null) return;
            if (UseCardView) { ShowOnCardView(instance ?? new CardInstance(definition, null)); return; }

            if (frameImage != null) frameImage.color = definition.FrameColor;
            if (nameText != null) nameText.text = Loc.CardName(definition.cardName);
            ApplyArtwork(definition.artwork);

            if (typeText != null)
            {
                string rarityTag = $"  ·  <color=#{ColorUtility.ToHtmlStringRGB(CardDefinition.RarityColor(definition.rarity))}>{Loc.T(CardDefinition.RarityName(definition.rarity))}</color>";
                switch (definition)
                {
                    case MonsterCardData monster:
                        int atk = instance != null ? instance.CurrentAtk : monster.atk;
                        int def = instance != null ? instance.CurrentDef : monster.def;
                        typeText.text = Loc.F("{0} • Level {1} • ATK {2} / DEF {3}", monster.AttributeTypeRichText(), monster.level, atk, def);
                        break;
                    case SpellCardData spell:
                        typeText.text = Loc.T(spell.speed == SpellSpeed.Quick ? "Quick Spell" : "Spell Card");
                        break;
                    case ArtifactCardData artifact:
                        string bonus = artifact.slot == ArtifactSlot.Monster ? $" • +{artifact.atkBonus} ATK / +{artifact.defBonus} DEF" : "";
                        typeText.text = $"{Loc.T("Artifact")} • {Loc.T(TcgCardView.ArtifactSlotName(artifact.slot))}{bonus}";
                        break;
                    case PlayerCardData playerCard:
                        typeText.text = Loc.F("Player Card • {0} starting LP", playerCard.startLifePoints);
                        break;
                    default:
                        typeText.text = "";
                        break;
                }
                typeText.text += rarityTag;
            }

            if (rulesText != null) rulesText.text = BuildRulesBody(definition, instance);
        }

        public void ShowHiddenCard()
        {
            if (UseCardView)
            {
                cardView.gameObject.SetActive(false);
                if (backPlate != null) backPlate.SetActive(true);
                if (caption != null) caption.SetActive(false);
                if (rarityChipBg != null) rarityChipBg.gameObject.SetActive(false);
                SetInspectRules($"<i>{Loc.T("You cannot see this card.")}</i>");
                return;
            }
            if (frameImage != null) frameImage.color = new Color(0.25f, 0.28f, 0.45f);
            ApplyArtwork(null);
            if (nameText != null) nameText.text = Loc.T("Face-down card");
            if (typeText != null) typeText.text = "";
            if (rulesText != null) rulesText.text = $"<i>{Loc.T("You cannot see this card.")}</i>";
        }

        /// <summary>
        /// Baut den Regeltext mit klar getrennten Effekt-Blöcken:
        /// farbiger NORMAL-/INFUSED-Header, Trigger-Zeile, Effekttext, Trennlinie.
        /// Public, damit auch der Deck Builder (Hover-Textbox) ihn nutzen kann.
        /// </summary>
        public static string BuildFormattedRulesText(CardDefinition definition)
        {
            // Vorspann: Beschwörungs-Bedingung (Reliquary/Selbst-Spezialbeschwörung) oder passive Feld-Aura
            string aura = "";
            if (definition is ReliquaryCardData reliquaryData && !string.IsNullOrWhiteSpace(reliquaryData.summonText))
                aura = $"<color=#F1E7D2><b>{Loc.T("RELIQUARY SUMMON")}</b></color> {CostChip(reliquaryData.summonManaCost)}\n{Loc.CardSummon(definition.cardName, reliquaryData.summonText)}";
            else if (definition is MonsterCardData monsterData)
            {
                string condition = monsterData.SelfSummonConditionText();
                if (!string.IsNullOrEmpty(condition))
                    aura = $"<color=#7ACD96><b>{Loc.T("SPECIAL SUMMON")}</b></color>\n{condition}";
            }
            // Dauerhafte Passiv-Fähigkeiten (Aura, Spott, Kampf-Schild, Rabatt ...)
            var passiveLines = definition.BuildPassiveLines();
            if (passiveLines.Count > 0)
            {
                string block = $"<color=#B08CFF><b>{Loc.T("PASSIVE")}</b></color>\n{string.Join("\n", passiveLines)}";
                aura = string.IsNullOrEmpty(aura) ? block : aura + "\n<color=#454B60>- - - - - - - - - - - - - -</color>\n" + block;
            }

            if (definition.effects == null || definition.effects.Count == 0)
                return string.IsNullOrEmpty(aura) ? $"<i>{Loc.T("No effect.")}</i>" : aura;

            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(aura)) sb.Append(aura).Append("\n<color=#454B60>- - - - - - - - - - - - - -</color>\n");
            bool first = true;
            foreach (var effect in definition.effects)
            {
                if (effect == null) continue;
                bool coupled = effect.isInfused && effect.infusedKind == InfusedKind.Coupled;
                if (!first)
                {
                    // Coupled-Upgrades hängen am Vorgänger: Verbinder statt Trennlinie
                    sb.Append(coupled
                        ? $"\n<color=#6FD3E0>{Loc.T("— or, instead —")}</color>\n"
                        : "\n<color=#454B60>- - - - - - - - - - - - - -</color>\n");
                }
                first = false;

                string kind = Loc.T(effect.isInfused ? (coupled ? "INFUSED UPGRADE" : "INFUSED") : "NORMAL");
                string headColor = effect.isInfused ? "#6FD3E0" : "#F0C33C";
                sb.Append($"<color={headColor}><b>{kind}</b></color> ").Append(CostChip(effect.manaCost));
                if (!string.IsNullOrEmpty(effect.label)) sb.Append($"<color={headColor}> · {Loc.CardLabel(definition.cardName, definition.effects.IndexOf(effect), effect.label)}</color>");

                string trigger = TriggerLabel(effect);
                if (effect.isInfused && !coupled)
                    trigger = string.IsNullOrEmpty(trigger) ? Loc.T("Standalone effect") : trigger + Loc.T(" · standalone");
                // Bewusst ohne Zahl: eine Gruppe darf mehrere Coupled-Effekte
                // haben, und "einer von beiden" wäre dann schlicht falsch.
                if (coupled)
                    trigger = string.IsNullOrEmpty(trigger)
                        ? Loc.T("Only one effect from this group each turn")
                        : trigger + Loc.T(" · only one from this group per turn");
                if (!string.IsNullOrEmpty(trigger)) sb.Append($"\n<size=80%><color=#9BA3B8>{trigger}</color></size>");

                if (!string.IsNullOrWhiteSpace(effect.text)) sb.Append('\n').Append(Loc.CardText(definition.cardName, definition.effects.IndexOf(effect), effect.text));
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Preisschild eines Effekts als farbiges Badge: blau mit Manazahl, grün
        /// wenn er nichts kostet. Der eingefärbte Hintergrund (mark) hebt die Kosten
        /// aus der Textwand heraus — man soll sie sehen, ohne zu lesen.
        /// </summary>
        public static string CostChip(int manaCost)
        {
            return manaCost > 0
                ? $"<mark=#12395CAA><color=#8FD8FF><b> {Loc.F("{0} MANA", manaCost)} </b></color></mark>"
                : $"<mark=#1B402AAA><color=#8FE0A8><b> {Loc.T("NO MANA")} </b></color></mark>";
        }

        /// <summary>Deutsche Kurzbeschreibung, wann ein Effekt aktiviert werden kann.</summary>
        private static string TriggerLabel(EffectDefinition effect)
        {
            string label;
            switch (effect.trigger)
            {
                case EffectTrigger.Ignition: label = Loc.T("Activate during your Main Phase"); break;
                case EffectTrigger.Quick: label = Loc.T("Response — activate any time"); break;
                case EffectTrigger.OnActivate: label = Loc.T("On play"); break;
                case EffectTrigger.OnSummonSelf: label = Loc.T("When this card is summoned"); break;
                case EffectTrigger.OnDestroyedSelf: label = Loc.T("When this card is destroyed"); break;
                case EffectTrigger.OnOpponentSummon: label = Loc.T("When your opponent summons"); break;
                case EffectTrigger.StandbyPhase: label = Loc.T("During your Standby Phase"); break;
                case EffectTrigger.EndPhase: label = Loc.T("During your End Phase"); break;
                case EffectTrigger.OnNormalSummonSelf: label = Loc.T("When this card is Normal Summoned"); break;
                case EffectTrigger.HandIgnition: label = Loc.T("Activate from your hand during your Main Phase"); break;
                case EffectTrigger.GraveyardIgnition: label = Loc.T("Activate from your Graveyard during your Main Phase"); break;
                case EffectTrigger.HandQuick: label = Loc.T("Response from your hand — activate any time"); break;
                case EffectTrigger.OnFlipFaceUp: label = Loc.T("FLIP — when this card is turned face-up"); break;
                default: label = ""; break;
            }
            if (effect.onlyIfSpecialSummoned && !string.IsNullOrEmpty(label)) label += Loc.T(" · only if Special Summoned");
            if (effect.oncePerTurn && !string.IsNullOrEmpty(label)) label += Loc.T(" · once per turn");
            return label;
        }
    }
}
