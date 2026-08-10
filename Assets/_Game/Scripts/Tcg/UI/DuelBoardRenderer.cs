using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Zeichnet den kompletten Duell-Zustand in die im Editor platzierten Zonen-Slots
    /// (voller Neuaufbau bei jeder Board-Änderung — robust und einfach).
    /// </summary>
    public class DuelBoardRenderer : MonoBehaviour
    {
        /// <summary>Statusanzeige einer Seite in der rechten Rail (Duel-Field-Design).</summary>
        [System.Serializable]
        public class StatusBinding
        {
            public TMP_Text nameText;
            public TMP_Text lpValue;
            public Image lpBarFill;
            public Transform manaContainer;
            public TMP_Text manaValue;   // grosse Zahl vor den Punkten ("3+2" bei Bonus-Mana)
            public TMP_Text manaCarry;   // "+1 MANA NEXT TURN" — nur sichtbar bei Übertrag
            public TMP_Text deckCount;
            public TMP_Text handCount;
            public TMP_Text gyCount;
            public TMP_Text banCount;
        }

        [Header("Kern-Referenzen")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("duel")]
        private DuelHost duelHost;
        [SerializeField] private TcgCardView cardViewPrefab;
        [SerializeField] private CardDetailPanel detailPanel;
        [SerializeField] private CardSkin skin;

        [Header("Kartengrößen (Duel-Field-Design)")]
        [SerializeField] private Vector2 fieldCardSize = new Vector2(112f, 157f);
        [SerializeField] private Vector2 handCardSize = new Vector2(120f, 168f);
        [SerializeField] private Vector2 foeHandCardSize = new Vector2(62f, 87f);

        [Header("Status-Rail (im Szenenbau verdrahtet)")]
        [SerializeField] private StatusBinding playerStatus = new StatusBinding();
        [SerializeField] private StatusBinding foeStatus = new StatusBinding();
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text activeNameText;
        [SerializeField] private Image[] phaseChipBgs = new Image[5];
        [SerializeField] private TMP_Text[] phaseChipTexts = new TMP_Text[5];
        [SerializeField] private TMP_Text dividerPhaseText;

        [Header("Spieler 1 (unten) — Zonen-Slots")]
        [SerializeField] private Transform[] p1MonsterSlots = new Transform[5];
        [SerializeField] private Transform[] p1SpellSlots = new Transform[3];
        [SerializeField] private Transform[] p1ArtifactSlots = new Transform[2];
        [SerializeField] private Transform p1PlayerSlot;
        [SerializeField] private Transform p1HandContainer;

        [Header("Spieler 2 (oben) — Zonen-Slots")]
        [SerializeField] private Transform[] p2MonsterSlots = new Transform[5];
        [SerializeField] private Transform[] p2SpellSlots = new Transform[3];
        [SerializeField] private Transform[] p2ArtifactSlots = new Transform[2];
        [SerializeField] private Transform p2PlayerSlot;
        [SerializeField] private Transform p2HandContainer;

        [Header("HUD-Texte")]
        [SerializeField] private TMP_Text p1InfoText;
        [SerializeField] private TMP_Text p2InfoText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text logText;

        [Header("Optionen")]
        [SerializeField, Tooltip("Anzahl der Log-Zeilen im HUD")] [Range(3, 300)] private int logLines = 9;
        [SerializeField, Tooltip("ScrollRect des Duel-Logs (Auto-Scroll ans Ende)")] private ScrollRect logScroll;

        public event Action<CardInstance> CardClicked;
        public event Action<CardInstance> CardDragStarted;
        public event Action<CardInstance, Vector2> CardDragEnded;
        public event Action AfterRebuild;

        private readonly Dictionary<CardInstance, TcgCardView> viewMap = new Dictionary<CardInstance, TcgCardView>();
        private readonly List<string> logBuffer = new List<string>();
        private readonly Dictionary<Image, Color> slotOriginalColors = new Dictionary<Image, Color>();
        private readonly Dictionary<Image, Sprite> slotOriginalSprites = new Dictionary<Image, Sprite>();
        private float p1DisplayedLp = -1f;
        private float p2DisplayedLp = -1f;

        /// <summary>Die Engine hinter dem Host — alle internen Zugriffe laufen hierüber.</summary>
        private DuelManager duel => duelHost != null ? duelHost.Duel : null;

        public DuelManager Duel => duel;

        /// <summary>Der Spieler der unteren Board-Hälfte (im Netzwerk-Duell immer der lokale).</summary>
        public PlayerState BottomPlayer =>
            duel == null ? null : (duel.LocalPlayer != null ? duel.LocalPlayer : duel.Player1);

        private void OnEnable()
        {
            if (duel == null) return;
            duel.OnBoardChanged += Rebuild;
            duel.OnPhaseChanged += UpdateHud;
            duel.OnLog += AppendLog;
            CardLinkText.Configure(detailPanel, duel.Catalog);
            CardLinkText.Attach(logText);
        }

        private void OnDisable()
        {
            if (duel == null) return;
            duel.OnBoardChanged -= Rebuild;
            duel.OnPhaseChanged -= UpdateHud;
            duel.OnLog -= AppendLog;
        }

        public bool TryGetView(CardInstance instance, out TcgCardView view) => viewMap.TryGetValue(instance, out view);

        public void SetHighlights(IEnumerable<CardInstance> cards, bool active)
        {
            if (cards == null) return;
            foreach (var card in cards)
                if (card != null && viewMap.TryGetValue(card, out var view)) view.SetHighlight(active);
        }

        public void ClearAllHighlights()
        {
            foreach (var view in viewMap.Values) view.SetHighlight(false);
        }

        /// <summary>Die Slot-Rechtecke einer Zonen-Reihe von Spieler 1 (für Drag-&-Drop-Erkennung).</summary>
        /// <summary>
        /// Der Monsterplatz einer Seite. Die Beschwörungs-Animation braucht ihn,
        /// bevor die Karte dort liegt — sie fliegt ja erst dorthin.
        /// </summary>
        public Transform GetMonsterSlot(bool localSide, int index)
        {
            var slots = localSide ? p1MonsterSlots : p2MonsterSlots;
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public IEnumerable<RectTransform> GetP1SlotRects(ZoneType zone)
        {
            Transform[] slots;
            switch (zone)
            {
                case ZoneType.MonsterZone: slots = p1MonsterSlots; break;
                case ZoneType.SpellZone: slots = p1SpellSlots; break;
                case ZoneType.ArtifactZone: slots = p1ArtifactSlots; break;
                default: yield break;
            }
            foreach (var slot in slots)
                if (slot != null) yield return (RectTransform)slot;
        }

        /// <summary>Hebt eine Zonen-Reihe von Spieler 1 als gültiges Drop-Ziel hervor.</summary>
        public void SetRowHighlight(ZoneType zone, bool active)
        {
            foreach (var rect in GetP1SlotRects(zone))
                ApplySlotHighlight(rect.GetComponent<Image>(), active);
        }

        public void ClearRowHighlights()
        {
            SetRowHighlight(ZoneType.MonsterZone, false);
            SetRowHighlight(ZoneType.SpellZone, false);
            SetRowHighlight(ZoneType.ArtifactZone, false);
        }

        /// <summary>Hebt nur bestimmte Slots einer Zonen-Reihe von Spieler 1 hervor (Zonen-Wahl).</summary>
        public void SetSlotHighlights(ZoneType zone, List<int> indices, bool active)
        {
            int index = 0;
            foreach (var rect in GetP1SlotRects(zone))
            {
                if (!active || indices.Contains(index))
                    ApplySlotHighlight(rect.GetComponent<Image>(), active);
                index++;
            }
        }

        /// <summary>Drop-Ziel-Optik: Sprite-Tausch auf das Gold-Zonen-Sprite (Fallback: grüne Färbung).</summary>
        private void ApplySlotHighlight(Image image, bool active)
        {
            if (image == null) return;
            if (skin != null && skin.zoneDropTarget != null)
            {
                if (!slotOriginalSprites.ContainsKey(image))
                {
                    slotOriginalSprites[image] = image.sprite;
                    slotOriginalColors[image] = image.color;
                }
                image.sprite = active ? skin.zoneDropTarget : slotOriginalSprites[image];
                image.color = active ? Color.white : slotOriginalColors[image];
            }
            else
            {
                if (!slotOriginalColors.ContainsKey(image)) slotOriginalColors[image] = image.color;
                image.color = active ? new Color(0.45f, 0.85f, 0.50f, 0.55f) : slotOriginalColors[image];
            }
        }

        public void Rebuild()
        {
            foreach (var view in viewMap.Values)
                if (view != null) Destroy(view.gameObject);
            viewMap.Clear();

            if (duel == null || duel.Player1 == null) return;

            var bottom = BottomPlayer;
            var top = bottom.Opponent;
            RenderSide(bottom, p1MonsterSlots, p1SpellSlots, p1ArtifactSlots, p1PlayerSlot, p1HandContainer, false);
            RenderSide(top, p2MonsterSlots, p2SpellSlots, p2ArtifactSlots, p2PlayerSlot, p2HandContainer, true);

            UpdateHud();
            AfterRebuild?.Invoke();
        }

        private void RenderSide(PlayerState player, Transform[] monsterSlots, Transform[] spellSlots,
            Transform[] artifactSlots, Transform playerSlot, Transform handContainer, bool isOpponent)
        {
            for (int i = 0; i < monsterSlots.Length && i < player.MonsterZones.Length; i++)
                if (player.MonsterZones[i] != null) SpawnView(player.MonsterZones[i], monsterSlots[i], false, fieldCardSize);

            for (int i = 0; i < spellSlots.Length && i < player.SpellZones.Length; i++)
            {
                var spell = player.SpellZones[i];
                if (spell != null) SpawnView(spell, spellSlots[i], isOpponent && spell.FaceDown, fieldCardSize);
            }

            for (int i = 0; i < artifactSlots.Length && i < player.ArtifactZones.Length; i++)
                if (player.ArtifactZones[i] != null) SpawnView(player.ArtifactZones[i], artifactSlots[i], false, fieldCardSize);

            if (player.PlayerCard != null && playerSlot != null)
                SpawnView(player.PlayerCard, playerSlot, false, fieldCardSize);

            if (handContainer != null)
                foreach (var card in player.Hand)
                    SpawnView(card, handContainer, isOpponent, isOpponent ? foeHandCardSize : handCardSize);
        }

        private void SpawnView(CardInstance instance, Transform parent, bool hideFace, Vector2 size)
        {
            if (parent == null || cardViewPrefab == null) return;
            var view = Instantiate(cardViewPrefab, parent);
            var rect = (RectTransform)view.transform;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            view.Show(instance, hideFace);
            view.HoverLift = parent == p1HandContainer && !hideFace; // nur eigene Handkarten heben sich
            view.Clicked += HandleClicked;
            view.Hovered += HandleHovered;
            view.DragStarted += HandleDragStarted;
            view.DragEnded += HandleDragEnded;
            viewMap[instance] = view;
        }

        private void HandleClicked(TcgCardView view)
        {
            if (view.Instance != null) CardClicked?.Invoke(view.Instance);
        }

        private void HandleDragStarted(TcgCardView view)
        {
            if (view.Instance != null) CardDragStarted?.Invoke(view.Instance);
        }

        private void HandleDragEnded(TcgCardView view, Vector2 screenPosition)
        {
            if (view.Instance != null) CardDragEnded?.Invoke(view.Instance, screenPosition);
        }

        private void HandleHovered(TcgCardView view)
        {
            if (detailPanel == null || view.Instance == null) return;
            // Eigene verdeckte Karten darf man beim Hovern einsehen
            bool mine = view.Instance.Owner == BottomPlayer;
            if (view.HiddenFace && !mine) detailPanel.ShowHiddenCard();
            else detailPanel.ShowCard(view.Instance);
        }

        /// <summary>
        /// Sind die angezeigten LP bei den echten Werten angekommen? Die End-
        /// Sequenz wartet darauf — der Kartensprung darf erst kommen, wenn die
        /// Null wirklich auf dem Schirm steht.
        /// </summary>
        public bool LpSettled =>
            duel == null || duel.Player1 == null
            || (Mathf.Approximately(p1DisplayedLp, BottomPlayer.LifePoints)
                && Mathf.Approximately(p2DisplayedLp, BottomPlayer.Opponent.LifePoints));

        /// <summary>Lässt die angezeigten LP animiert zum echten Wert ticken.</summary>
        private void Update()
        {
            if (duel == null || duel.Player1 == null) return;
            var bottom = BottomPlayer;
            bool changed = TickLp(ref p1DisplayedLp, bottom.LifePoints);
            if (TickLp(ref p2DisplayedLp, bottom.Opponent.LifePoints)) changed = true;
            if (changed) UpdateHud();
        }

        private static bool TickLp(ref float displayed, int actual)
        {
            if (displayed < 0f) { displayed = actual; return true; }
            if (Mathf.Approximately(displayed, actual)) return false;
            float speed = Mathf.Max(600f, Mathf.Abs(actual - displayed) * 4f);
            displayed = Mathf.MoveTowards(displayed, actual, speed * Time.deltaTime);
            return true;
        }

        public void UpdateHud()
        {
            if (duel == null || duel.Player1 == null) return;

            var bottomPlayer = BottomPlayer;
            if (p1InfoText != null) p1InfoText.text = BuildInfo(bottomPlayer, p1DisplayedLp);
            if (p2InfoText != null) p2InfoText.text = BuildInfo(bottomPlayer.Opponent, p2DisplayedLp);
            if (phaseText != null)
            {
                string turnMarker = duel.TurnPlayer != null ? duel.TurnPlayer.Name : "-";
                phaseText.text = $"Turn {duel.TurnNumber} — {turnMarker}\n{PhaseName(duel.Phase)}";
            }

            // ---- Duel-Field-Rail ----
            UpdateStatusPanel(playerStatus, bottomPlayer, p1DisplayedLp, new Color32(0xEB, 0xCE, 0x8A, 0xFF));
            UpdateStatusPanel(foeStatus, bottomPlayer.Opponent, p2DisplayedLp, new Color32(0x8F, 0xC6, 0xD2, 0xFF));

            if (turnText != null) turnText.text = $"Turn {duel.TurnNumber}";
            if (activeNameText != null) activeNameText.text = duel.TurnPlayer != null ? duel.TurnPlayer.Name : "";
            int phaseIndex = (int)duel.Phase; // Draw, Standby, Main, Battle, End
            for (int i = 0; i < phaseChipBgs.Length; i++)
            {
                bool active = i == phaseIndex;
                if (phaseChipBgs[i] != null)
                {
                    phaseChipBgs[i].sprite = active && skin != null ? skin.badgeMonster : null;
                    phaseChipBgs[i].color = active ? Color.white : new Color(0f, 0f, 0f, 0.4f);
                }
                if (i < phaseChipTexts.Length && phaseChipTexts[i] != null)
                    phaseChipTexts[i].color = active ? new Color32(0x1E, 0x14, 0x05, 0xFF) : new Color32(0x6A, 0x5E, 0x4A, 0xFF);
            }

            if (dividerPhaseText != null)
            {
                bool mine = duel.TurnPlayer == bottomPlayer;
                dividerPhaseText.text = $"{(mine ? "YOUR" : "FOE")} {PhaseName(duel.Phase).ToUpperInvariant()}";
            }
        }

        /// <summary>Temporär gewonnenes Mana — dasselbe Grün wie LP-Heilung.</summary>
        private static readonly Color TempManaColor = new Color32(0x7D, 0xDB, 0x6E, 0xFF);

        private readonly Dictionary<PlayerState, int> maxLpCache = new Dictionary<PlayerState, int>();

        private void UpdateStatusPanel(StatusBinding binding, PlayerState player, float displayedLp, Color pipColor)
        {
            if (binding == null || player == null) return;
            int lp = displayedLp < 0f ? player.LifePoints : Mathf.RoundToInt(displayedLp);
            if (!maxLpCache.TryGetValue(player, out int maxLp)) maxLpCache[player] = maxLp = player.LifePoints;
            else if (player.LifePoints > maxLp) maxLpCache[player] = maxLp = player.LifePoints;

            if (binding.nameText != null) binding.nameText.text = player.Name;
            if (binding.lpValue != null)
            {
                string lpColor = lp > player.LifePoints ? "#E0603A" : lp < player.LifePoints ? "#7DDB6E" : "#FFFFFF";
                binding.lpValue.text = $"<color={lpColor}>{lp}</color>";
            }
            if (binding.lpBarFill != null && maxLp > 0)
                binding.lpBarFill.fillAmount = Mathf.Clamp01(lp / (float)maxLp);   // tickt mit der Zahl
            // Runden-Basis = reguläres Mana + dauerhafter Bonus (Tower-Bots). Alles
            // darüber ist temporär gewonnen (King's Crown, Mana-Diebstahl) und
            // leuchtet grün — es verfällt mit dem nächsten Auffüllen.
            int manaBase = player.ManaPerTurn + player.BonusManaPerTurn;
            int tempMana = Mathf.Max(0, player.Mana - manaBase);
            if (binding.manaContainer != null)
            {
                int totalPips = Mathf.Max(manaBase, player.Mana);
                int pipIndex = 0; // die Mana-Zahl lebt im selben Container — nur echte Pips (Images) zählen
                for (int i = 0; i < binding.manaContainer.childCount; i++)
                {
                    var child = binding.manaContainer.GetChild(i);
                    var image = child.GetComponent<Image>();
                    if (image == null) continue;
                    bool exists = pipIndex < totalPips;
                    if (child.gameObject.activeSelf != exists) child.gameObject.SetActive(exists);
                    if (exists)
                    {
                        var baseColor = pipIndex >= manaBase ? TempManaColor : pipColor;
                        bool available = pipIndex < player.Mana;
                        image.color = available ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
                    }
                    pipIndex++;
                }
            }
            if (binding.manaValue != null)
                binding.manaValue.text = tempMana > 0
                    ? $"{player.Mana - tempMana}<color=#7DDB6E>+{tempMana}</color>"
                    : player.Mana.ToString();
            if (binding.manaCarry != null)
            {
                int carry = player.ManaCredit - player.ManaDebt;
                if (binding.manaCarry.gameObject.activeSelf != (carry != 0))
                    binding.manaCarry.gameObject.SetActive(carry != 0);
                if (carry != 0)
                    binding.manaCarry.text = carry > 0
                        ? $"<color=#7DDB6E>+{carry} MANA NEXT TURN</color>"
                        : $"<color=#E8695E>{carry} MANA NEXT TURN</color>";
            }
            if (binding.deckCount != null) binding.deckCount.text = player.DeckPile.Count.ToString();
            if (binding.handCount != null) binding.handCount.text = player.Hand.Count.ToString();
            if (binding.gyCount != null) binding.gyCount.text = player.Graveyard.Count.ToString();
            if (binding.banCount != null) binding.banCount.text = player.Banished.Count.ToString();
        }

        private string BuildInfo(PlayerState player, float displayedLp)
        {
            int lp = displayedLp < 0f ? player.LifePoints : Mathf.RoundToInt(displayedLp);
            string lpColor = lp > player.LifePoints ? "#E8695E" : (lp < player.LifePoints ? "#7DDB6E" : "#FFFFFF");
            return $"{player.Name}\nLP: <color={lpColor}>{lp}</color>\nMana: {player.Mana} / {player.ManaPerTurn}\n" +
                   $"Deck: {player.DeckPile.Count} • Hand: {player.Hand.Count}\n" +
                   $"Graveyard: {player.Graveyard.Count} • Banished: {player.Banished.Count}";
        }

        public static string PhaseName(DuelPhase phase) => DuelManager.PhaseName(phase);

        private void AppendLog(string message)
        {
            logBuffer.Add(FormatLogLine(CardLinkText.Linkify(message, onLightBackground: true)));
            while (logBuffer.Count > logLines) logBuffer.RemoveAt(0);
            if (logText == null) return;
            logText.text = string.Join("\n", logBuffer);
            if (logScroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)logText.transform);
                logScroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Das Log-Panel ist helles Pergament — deshalb durchgehend DUNKLE Tinte.
        /// Turn-Marker in kräftigem Bronze, normale Aktionen mit Bullet und hängendem
        /// Einzug; Kartennamen bringen aus Linkify() ihre dunkle Typfarbe mit und
        /// überschreiben den Fließtext nur für ihre eigene Länge.
        /// </summary>
        private static string FormatLogLine(string message)
        {
            if (message.StartsWith("—") || message.StartsWith("Duel:"))
                return $"\n<color=#4A3608><b>{message}</b></color>";
            // Dunkle Tinte auf dem hellen Pergament — #3A3020 war zu blass,
            // gerade bei kleinen Grössen. Lesbarkeit schlaegt Eleganz.
            return $"<color=#8A6A28>◆</color> <indent=14><color=#1E1508>{message}</color></indent>";
        }
    }
}
