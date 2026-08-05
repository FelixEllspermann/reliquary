using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Verbindet den menschlichen Spieler mit der Engine: nimmt Requests entgegen,
    /// zeigt Prompts/Highlights und übersetzt Klicks in Antworten.
    /// </summary>
    public class DuelUIController : MonoBehaviour, IDuelUi
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private DuelBoardRenderer board;
        [SerializeField] private PromptPanel promptPanel;
        [SerializeField] private CardDetailPanel detailPanel;
        [SerializeField] private DuelPileViewer pileViewer;   // Karten-Grid für Deck-/Friedhof-Auswahlen
        [SerializeField] private TMP_Text statusText;

        [Header("Phasen-Buttons")]
        [SerializeField] private Button battlePhaseButton;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button endBattleButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button surrenderButton;   // immer sichtbar, mit Rückfrage

        private MainActionRequest currentMain;
        private BattleActionRequest currentBattle;
        private TargetRequest currentTarget;
        private readonly List<CardInstance> pickedTargets = new List<CardInstance>();

        // Angriff in zwei Schritten: erst der Angreifer, dann sein Ziel auf dem Feld
        private CardInstance pendingAttacker;
        private readonly List<int> pendingAttackOptions = new List<int>();

        // Laufzeit-Kopie des Cancel-Knopfs — bestätigt eine unvollständige "bis zu"-Auswahl
        private Button confirmButton;
        private TMP_Text confirmLabel;

        private static readonly Color AttackerTint = new Color(1f, 0.72f, 0.35f, 0.95f);

        private void Awake()
        {
            CardLinkText.Attach(statusText);
            if (battlePhaseButton != null) battlePhaseButton.onClick.AddListener(OnBattlePhaseButton);
            if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnButton);
            if (endBattleButton != null) endBattleButton.onClick.AddListener(OnEndBattleButton);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButton);
            if (surrenderButton != null) surrenderButton.onClick.AddListener(OnSurrenderButton);

            BuildConfirmButton();

            // Die Duell-Knöpfe tragen die wichtigsten Entscheidungen — deutlich kräftigeres Feedback
            Strengthen(battlePhaseButton);
            Strengthen(endTurnButton);
            Strengthen(endBattleButton);
            Strengthen(cancelButton);
            Strengthen(surrenderButton);
            HideAllControls();
        }

        private static void Strengthen(Button button)
        {
            if (button == null) return;
            var fx = UiButtonFx.Attach(button);
            if (fx != null) fx.SetStrength(1.1f, 0.36f);
        }

        /// <summary>
        /// Bestätigen-Knopf für "bis zu"-Auswahlen: eine Kopie des Cancel-Knopfs, die auf
        /// dem Platz der Phasen-Knöpfe sitzt — die sind während einer Zielwahl ohnehin aus.
        /// </summary>
        private void BuildConfirmButton()
        {
            if (cancelButton == null) return;

            var copy = Instantiate(cancelButton.gameObject, cancelButton.transform.parent);
            copy.name = "ConfirmButton";
            foreach (var junk in new[] { "Glow", "~FxGlow" })
            {
                var child = copy.transform.Find(junk);
                if (child != null) Destroy(child.gameObject);
            }
            var inherited = copy.GetComponent<UiButtonFx>();
            if (inherited != null) Destroy(inherited);

            var rect = (RectTransform)copy.transform;
            float y = battlePhaseButton != null
                ? ((RectTransform)battlePhaseButton.transform).anchoredPosition.y
                : rect.anchoredPosition.y + 41f;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

            confirmButton = copy.GetComponent<Button>();
            confirmLabel = copy.GetComponentInChildren<TMP_Text>(true);
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmButton);
                Strengthen(confirmButton);
            }
            copy.SetActive(false);
        }

        /// <summary>Aufgeben — mit Rückfrage, damit niemand versehentlich das Duell wegwirft.</summary>
        private void OnSurrenderButton()
        {
            if (promptPanel == null || board == null || board.Duel == null) return;
            if (promptPanel.IsOpen) return;
            if (board.Duel.Result != DuelResult.None) return;

            promptPanel.ShowYesNo("Surrender", "Give up this duel? Your opponent takes the win.", confirmed =>
            {
                if (!confirmed) return;
                var duel = board.Duel;
                if (duel == null || duel.Result != DuelResult.None) return;
                var me = duel.LocalPlayer != null ? duel.LocalPlayer : board.BottomPlayer;
                if (me == null) return;
                // Server-Duell: der Server wertet das Verlassen als Aufgabe und meldet das Ende
                if (Net.MatchContext.IsServerMatch)
                {
                    if (Net.NetworkManager.Instance != null) Net.NetworkManager.Instance.SendLeave();
                    return;
                }
                duel.Log($"{me.Name} surrenders.");
                duel.Forfeit(me);
            });
        }

        // ================== EXTRA DECK (vom Feld-Stapel des DuelPileViewer genutzt) ==================

        /// <summary>True, solange in der aktuellen Main Phase mindestens eine Reliquary beschworen werden kann.</summary>
        public bool HasReliquarySummon()
        {
            return currentMain != null && !currentMain.Answered
                && currentMain.Options.Any(o => o.Kind == MainActionKind.SummonReliquary);
        }

        /// <summary>True, wenn genau diese Reliquary-Karte gerade beschworen werden kann.</summary>
        public bool CanReliquarySummon(CardInstance card)
        {
            if (card == null || currentMain == null || currentMain.Answered) return false;
            if (promptPanel != null && promptPanel.IsOpen) return false;
            return currentMain.Options.Any(o => o.Kind == MainActionKind.SummonReliquary
                && o.Card != null && o.Card.Definition == card.Definition);
        }

        /// <summary>Wählt die Reliquary-Beschwörung dieser Karte als Main-Aktion (Klick im Extra-Deck-Overlay).</summary>
        public bool TryChooseReliquarySummon(CardInstance card)
        {
            var request = currentMain;
            if (card == null || request == null || request.Answered) return false;
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                if (option.Kind != MainActionKind.SummonReliquary || option.Card == null) continue;
                if (option.Card.Definition != card.Definition) continue;
                request.Chosen = i;
                request.Answered = true;
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            if (board != null)
            {
                board.CardClicked += OnCardClicked;
                board.CardDragStarted += OnCardDragStarted;
                board.CardDragEnded += OnCardDragEnded;
                board.AfterRebuild += ReapplyHighlights;
            }
        }

        private void OnDisable()
        {
            if (board != null)
            {
                board.CardClicked -= OnCardClicked;
                board.CardDragStarted -= OnCardDragStarted;
                board.CardDragEnded -= OnCardDragEnded;
                board.AfterRebuild -= ReapplyHighlights;
            }
        }

        /// <summary>
        /// Zeigt an, dass der Gegner gerade eine Entscheidung trifft. Ein stilles
        /// Brett und ein hängendes Spiel sehen sonst gleich aus — gerade wenn er
        /// mitten in DEINEM Zug entscheidet, ob er auf etwas reagiert.
        /// Leerer Text nimmt den Hinweis wieder weg.
        /// </summary>
        public void ShowOpponentThinking(string what)
        {
            if (string.IsNullOrEmpty(what)) { SetStatus(""); return; }
            var bottom = board != null ? board.BottomPlayer : null;
            string foe = bottom != null && bottom.Opponent != null ? bottom.Opponent.Name : "Opponent";
            SetStatus($"{foe} is {what}…");
        }

        private void HideAllControls()
        {
            if (battlePhaseButton != null) battlePhaseButton.gameObject.SetActive(false);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
            if (endBattleButton != null) endBattleButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            SetStatus("");
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = CardLinkText.Linkify(text);
        }

        // ================== REQUEST-HANDLER ==================

        public IEnumerator Handle(MainActionRequest request)
        {
            currentMain = request;
            SetStatus("Your Main Phase — click a card or drag it onto a zone.");
            if (battlePhaseButton != null)
                battlePhaseButton.gameObject.SetActive(request.Options.Any(o => o.Kind == MainActionKind.ToBattlePhase));
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(true);
            ApplyPlayableOutlines();

            while (!request.Answered) yield return null;

            currentMain = null;
            board.ClearAllHighlights();
            board.ClearRowHighlights();
            HideAllControls();
        }

        public IEnumerator Handle(BattleActionRequest request)
        {
            currentBattle = request;
            pendingAttacker = null;
            pendingAttackOptions.Clear();
            SetStatus("Battle Phase — click a monster that can attack.");
            if (endBattleButton != null) endBattleButton.gameObject.SetActive(true);

            var attackers = request.Options.Where(o => !o.EndBattle).Select(o => o.Attacker).Distinct().ToList();
            board.SetHighlights(attackers, true);

            while (!request.Answered) yield return null;

            currentBattle = null;
            pendingAttacker = null;
            pendingAttackOptions.Clear();
            board.ClearAllHighlights();
            HideAllControls();
        }

        /// <summary>Münzwurf-Gewinner wählt First/Second (callback true = zuerst).</summary>
        public void AskStartChoice(System.Action<bool> callback)
        {
            if (promptPanel == null) { callback?.Invoke(true); return; }
            promptPanel.ShowOptions("Coin Toss", "You won the toss — choose your position.",
                new System.Collections.Generic.List<string> { "Go first", "Go second" }, false,
                index => callback?.Invoke(index == 0));
        }

        public IEnumerator Handle(YesNoRequest request)
        {
            if (request.Card != null && detailPanel != null) detailPanel.ShowCard(request.Card);
            TcgCardView askedView = null;
            if (request.Card != null && board.TryGetView(request.Card, out askedView))
                askedView.SetHighlight(true);

            bool done = false;
            promptPanel.ShowYesNo(request.Title, request.Question, result =>
            {
                request.Result = result;
                request.Answered = true;
                done = true;
            });
            while (!done) yield return null;

            if (askedView != null) askedView.SetHighlight(false);
        }

        public IEnumerator Handle(OptionRequest request)
        {
            if (request.Card != null && detailPanel != null) detailPanel.ShowCard(request.Card);
            bool done = false;
            promptPanel.ShowOptions(request.Title, "", request.Options, request.AllowCancel, result =>
            {
                request.Result = result;
                request.Answered = true;
                done = true;
            });
            while (!done) yield return null;
        }

        public IEnumerator Handle(ZoneSelectRequest request)
        {
            // Zonen-Wahl gilt nur für die eigene (untere) Board-Hälfte
            if (request.ForPlayer != board.BottomPlayer)
            {
                request.Result = request.FreeIndices.Count > 0 ? request.FreeIndices[0] : -1;
                request.Answered = true;
                yield break;
            }

            SetStatus($"{request.Title} — click a highlighted zone.");
            board.SetSlotHighlights(request.Zone, request.FreeIndices, true);

            var mouse = UnityEngine.InputSystem.Mouse.current;
            while (!request.Answered)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    int index = GetSlotIndexUnder(request.Zone, mouse.position.ReadValue());
                    if (index >= 0 && request.FreeIndices.Contains(index))
                    {
                        request.Result = index;
                        request.Answered = true;
                    }
                }
                yield return null;
            }

            board.SetSlotHighlights(request.Zone, request.FreeIndices, false);
            SetStatus("");
        }

        public IEnumerator Handle(TargetRequest request)
        {
            // Nicht sichtbare Kandidaten (Deck/Friedhof/Banishment): scrollbares Karten-Grid
            // mit echten Kartenansichten — Klick wählt, Hover zeigt das Detail-Panel.
            bool anyVisible = request.Candidates.Any(c => board.TryGetView(c, out _));
            if (!anyVisible && pileViewer != null)
            {
                yield return pileViewer.PickCards(request);
                yield break;
            }
            if (!anyVisible)
            {
                var remaining = new List<CardInstance>(request.Candidates);
                while (request.Result.Count < request.Count && remaining.Count > 0)
                {
                    var labels = remaining
                        .Select(c => c.MonsterData != null ? $"{c.Name} (ATK {c.MonsterData.atk})" : c.Name)
                        .ToList();
                    string title = request.Count > 1
                        ? $"{request.Title} ({request.Count - request.Result.Count} more)"
                        : request.Title;
                    bool done = false;
                    int picked = -1;
                    promptPanel.ShowOptions(title, "", labels, request.AllowCancel, result =>
                    {
                        picked = result;
                        done = true;
                    });
                    while (!done) yield return null;

                    if (picked < 0 || picked >= remaining.Count)
                    {
                        request.Cancelled = request.AllowCancel;
                        break;
                    }
                    request.Result.Add(remaining[picked]);
                    remaining.RemoveAt(picked);
                }
                request.Answered = true;
                yield break;
            }

            currentTarget = request;
            pickedTargets.Clear();
            SetStatus(request.AllowFewer
                ? $"{request.Title} — up to {request.Count}, confirm when done"
                : $"{request.Title} ({request.Count})");
            board.SetHighlights(request.Candidates, true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(request.AllowCancel);
            if (confirmButton != null) confirmButton.gameObject.SetActive(request.AllowFewer);
            RefreshConfirmButton();

            while (!request.Answered) yield return null;

            currentTarget = null;
            board.ClearAllHighlights();
            HideAllControls();
        }

        private void ReapplyHighlights()
        {
            if (pendingAttacker != null && currentBattle != null)
            {
                ShowAttackTargets();
            }
            else if (currentTarget != null)
            {
                board.SetHighlights(currentTarget.Candidates, true);
            }
            else if (currentBattle != null)
            {
                var attackers = currentBattle.Options.Where(o => !o.EndBattle).Select(o => o.Attacker).Distinct();
                board.SetHighlights(attackers, true);
            }
            else if (currentMain != null)
            {
                ApplyPlayableOutlines();
            }
        }

        /// <summary>Grüne Outline auf allen Karten, die gerade eine legale Aktion haben.</summary>
        private void ApplyPlayableOutlines()
        {
            if (currentMain == null) return;
            foreach (var card in currentMain.Options.Where(o => o.Card != null).Select(o => o.Card).Distinct())
            {
                if (board.TryGetView(card, out var view))
                    view.SetHighlight(true, TcgCardView.PlayableHighlight);
            }
        }

        // ================== DRAG & DROP ==================

        private void OnCardDragStarted(CardInstance instance)
        {
            if (currentMain == null) return;
            if (promptPanel != null && promptPanel.IsOpen) return;

            bool hasSummon = currentMain.Options.Any(o => o.Card == instance &&
                (o.Kind == MainActionKind.SummonMonster || o.Kind == MainActionKind.SpecialSummonSelf));
            bool hasSpell = currentMain.Options.Any(o => o.Card == instance &&
                (o.Kind == MainActionKind.ActivateSpellFromHand || o.Kind == MainActionKind.SetSpell));
            bool hasArtifact = currentMain.Options.Any(o => o.Card == instance && o.Kind == MainActionKind.PlayArtifact);

            if (hasSummon) board.SetRowHighlight(ZoneType.MonsterZone, true);
            if (hasSpell) board.SetRowHighlight(ZoneType.SpellZone, true);
            if (hasArtifact) board.SetRowHighlight(ZoneType.ArtifactZone, true);
        }

        private void OnCardDragEnded(CardInstance instance, Vector2 screenPosition)
        {
            board.ClearRowHighlights();
            var request = currentMain;
            board.Rebuild(); // gedraggte Karte zurück ins Hand-Layout
            if (request == null || request.Answered) return;
            if (promptPanel != null && promptPanel.IsOpen) return;

            List<int> indices = null;
            int slotIndex = GetSlotIndexUnder(ZoneType.MonsterZone, screenPosition);
            if (slotIndex >= 0)
            {
                indices = OptionIndicesFor(request, instance, MainActionKind.SummonMonster, MainActionKind.SpecialSummonSelf);
            }
            else
            {
                slotIndex = GetSlotIndexUnder(ZoneType.SpellZone, screenPosition);
                if (slotIndex >= 0)
                {
                    indices = OptionIndicesFor(request, instance, MainActionKind.ActivateSpellFromHand, MainActionKind.SetSpell);
                }
                else
                {
                    slotIndex = GetSlotIndexUnder(ZoneType.ArtifactZone, screenPosition);
                    if (slotIndex >= 0)
                        indices = OptionIndicesFor(request, instance, MainActionKind.PlayArtifact);
                }
            }

            if (indices == null || indices.Count == 0) return;
            int droppedSlot = slotIndex;

            if (indices.Count == 1)
            {
                request.Options[indices[0]].PreferredZoneIndex = droppedSlot;
                request.Chosen = indices[0];
                request.Answered = true;
                return;
            }

            var labels = indices.Select(i => request.Options[i].Label).ToList();
            promptPanel.ShowOptions($"{instance.Name}: choose action", "", labels, true, result =>
            {
                if (result >= 0 && result < indices.Count && request == currentMain && !request.Answered)
                {
                    request.Options[indices[result]].PreferredZoneIndex = droppedSlot;
                    request.Chosen = indices[result];
                    request.Answered = true;
                }
            });
        }

        /// <summary>Index des Zonen-Slots unter dem Mauszeiger, oder -1.</summary>
        private int GetSlotIndexUnder(ZoneType zone, Vector2 screenPosition)
        {
            int index = 0;
            foreach (var rect in board.GetP1SlotRects(zone))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
                    return index;
                index++;
            }
            return -1;
        }

        private static List<int> OptionIndicesFor(MainActionRequest request, CardInstance card, params MainActionKind[] kinds)
        {
            var indices = new List<int>();
            for (int i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                if (option.Card == card && kinds.Contains(option.Kind)) indices.Add(i);
            }
            return indices;
        }

        // ================== KLICK-LOGIK ==================

        private void OnCardClicked(CardInstance instance)
        {
            if (promptPanel != null && promptPanel.IsOpen) return;

            if (currentTarget != null)
            {
                HandleTargetClick(instance);
                return;
            }

            if (currentBattle != null)
            {
                HandleBattleClick(instance);
                return;
            }

            if (currentMain != null)
            {
                HandleMainClick(instance);
            }
        }

        private void HandleTargetClick(CardInstance instance)
        {
            if (!currentTarget.Candidates.Contains(instance) || pickedTargets.Contains(instance)) return;

            pickedTargets.Add(instance);
            if (board.TryGetView(instance, out var view)) view.SetHighlight(false);

            if (pickedTargets.Count >= currentTarget.Count)
            {
                currentTarget.Result.AddRange(pickedTargets);
                currentTarget.Answered = true;
                return;
            }

            SetStatus(currentTarget.AllowFewer
                ? $"{currentTarget.Title} — {pickedTargets.Count} of up to {currentTarget.Count} chosen"
                : $"{currentTarget.Title} ({currentTarget.Count - pickedTargets.Count} more)");
            RefreshConfirmButton();
        }

        /// <summary>Bestätigen ist erst möglich, wenn mindestens ein Ziel gewählt wurde.</summary>
        private void RefreshConfirmButton()
        {
            if (confirmButton == null || !confirmButton.gameObject.activeSelf) return;
            bool ready = pickedTargets.Count > 0;
            confirmButton.interactable = ready;
            if (confirmLabel != null)
                confirmLabel.text = ready ? $"CONFIRM ({pickedTargets.Count})" : "CONFIRM";
        }

        private void OnConfirmButton()
        {
            if (currentTarget == null || !currentTarget.AllowFewer || pickedTargets.Count == 0) return;
            currentTarget.Result.AddRange(pickedTargets);
            currentTarget.Answered = true;
        }

        // ---------- Angriff: erst der Angreifer, dann sein Ziel auf dem Feld ----------

        private void HandleBattleClick(CardInstance instance)
        {
            // Zweiter Klick — ein hervorgehobenes Ziel wählen oder die Auswahl aufheben
            if (pendingAttacker != null)
            {
                foreach (int index in pendingAttackOptions)
                {
                    if (currentBattle.Options[index].Target != instance) continue;
                    ChooseBattleOption(index);
                    return;
                }
                if (instance == pendingAttacker)
                {
                    ClearPendingAttack();
                    return;
                }
            }

            var indices = new List<int>();
            for (int i = 0; i < currentBattle.Options.Count; i++)
                if (!currentBattle.Options[i].EndBattle && currentBattle.Options[i].Attacker == instance)
                    indices.Add(i);
            if (indices.Count == 0) return;

            // Ein einziges Angebot (Direktangriff) läuft ohne Rückfrage
            if (indices.Count == 1)
            {
                ChooseBattleOption(indices[0]);
                return;
            }

            pendingAttacker = instance;
            pendingAttackOptions.Clear();
            pendingAttackOptions.AddRange(indices);
            ShowAttackTargets();
        }

        private void ShowAttackTargets()
        {
            board.ClearAllHighlights();
            var targets = new List<CardInstance>();
            foreach (int index in pendingAttackOptions)
            {
                var target = currentBattle.Options[index].Target;
                if (target != null) targets.Add(target);
            }
            board.SetHighlights(targets, true);
            if (board.TryGetView(pendingAttacker, out var attackerView))
                attackerView.SetHighlight(true, AttackerTint);

            SetStatus($"{pendingAttacker.Name} attacks — click a highlighted target (or the attacker to cancel).");
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        }

        private void ClearPendingAttack()
        {
            if (pendingAttacker == null) return;
            pendingAttacker = null;
            pendingAttackOptions.Clear();
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (currentBattle != null)
            {
                SetStatus("Battle Phase — click a monster that can attack.");
                ReapplyHighlights();
            }
        }

        private void ChooseBattleOption(int index)
        {
            var request = currentBattle;
            if (request == null || request.Answered) return;
            pendingAttacker = null;
            pendingAttackOptions.Clear();
            request.Chosen = index;
            request.Answered = true;
        }

        private void HandleMainClick(CardInstance instance)
        {
            var indices = new List<int>();
            for (int i = 0; i < currentMain.Options.Count; i++)
            {
                var option = currentMain.Options[i];
                if (option.Card == instance) indices.Add(i);
            }
            if (indices.Count == 0) return;

            var labels = indices.Select(i => currentMain.Options[i].Label).ToList();
            var request = currentMain;
            promptPanel.ShowOptions($"{instance.Name}: choose action", "", labels, true, result =>
            {
                if (result >= 0 && result < indices.Count && request == currentMain && !request.Answered)
                {
                    request.Chosen = indices[result];
                    request.Answered = true;
                }
            });
        }

        // ================== BUTTONS ==================

        private void OnBattlePhaseButton()
        {
            if (currentMain == null) return;
            int index = currentMain.Options.FindIndex(o => o.Kind == MainActionKind.ToBattlePhase);
            if (index >= 0) { currentMain.Chosen = index; currentMain.Answered = true; }
        }

        private void OnEndTurnButton()
        {
            if (currentMain == null) return;
            int index = currentMain.Options.FindIndex(o => o.Kind == MainActionKind.EndTurn);
            if (index >= 0) { currentMain.Chosen = index; currentMain.Answered = true; }
        }

        private void OnEndBattleButton()
        {
            if (currentBattle == null) return;
            int index = currentBattle.Options.FindIndex(o => o.EndBattle);
            if (index >= 0) { currentBattle.Chosen = index; currentBattle.Answered = true; }
        }

        private void OnCancelButton()
        {
            if (pendingAttacker != null)
            {
                ClearPendingAttack();
                return;
            }
            if (currentTarget != null && currentTarget.AllowCancel)
            {
                currentTarget.Cancelled = true;
                currentTarget.Answered = true;
            }
        }
    }
}
