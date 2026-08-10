using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Decks-Bereich: mehrere Account-Decks anlegen, bearbeiten, löschen (Server-gespeichert)
    /// sowie Karten craften/entcraften. Ohne Login: Ansicht des Starter-Decks.
    /// </summary>
    public class DeckEditorController : MonoBehaviour
    {
        private const int CraftCost = 30; // Anzeige-Wert; die Wahrheit liegt beim Server

        [Header("Daten (im Inspector verdrahten)")]
        [SerializeField, Tooltip("Alle Karten, die im Pool angeboten werden")]
        private List<CardDefinition> cardPool = new List<CardDefinition>();

        [SerializeField, Tooltip("Wählbare Spielerkarten (Helden)")]
        private List<PlayerCardData> playerCards = new List<PlayerCardData>();

        [SerializeField, Tooltip("Regelwerk für Deckgröße und Kopien-Limit")]
        private GameRules rules;

        [SerializeField, Tooltip("Anzeige-Deck, wenn kein Konto eingeloggt ist")]
        private DeckDefinition fallbackDeck;

        [SerializeField, Tooltip("Karten-Katalog (Namen → Assets)")]
        private CardCatalog catalog;

        [Header("UI-Referenzen")]
        [SerializeField] private TMP_Dropdown deckDropdown;
        [SerializeField] private TMP_InputField deckNameInput;
        [SerializeField] private TMP_Dropdown playerCardDropdown;
        [SerializeField] private Transform poolContent;
        [SerializeField] private Transform deckContent;
        [SerializeField] private DeckEditorRow rowPrefab;
        [SerializeField] private CardDetailPanel detailPanel;
        [SerializeField] private TMP_Text deckInfoText;
        [SerializeField] private TMP_Text tokensText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button newDeckButton;
        [SerializeField] private Button deleteDeckButton;
        [SerializeField] private Button backButton;

        [Header("Einstellungen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private RuntimeDeck offlineDeck;
        private NetworkManager network;

        private bool CollectionMode =>
            PlayerProfile.LoggedIn && network != null && network.IsConnected;

        private int MaxCopies => rules != null ? rules.maxCopiesPerCard : 3;
        private int DeckMin => rules != null ? rules.deckMinSize : 40;
        private int DeckMax => rules != null ? rules.deckMaxSize : 80;

        private int CurrentIndex => deckDropdown != null ? deckDropdown.value : 0;

        private RuntimeDeck CurrentDeck
        {
            get
            {
                if (CollectionMode)
                {
                    if (PlayerProfile.Decks.Count == 0) return null;
                    return PlayerProfile.Decks[Mathf.Clamp(CurrentIndex, 0, PlayerProfile.Decks.Count - 1)];
                }
                if (offlineDeck == null && fallbackDeck != null)
                {
                    offlineDeck = new RuntimeDeck
                    {
                        Name = fallbackDeck.deckName,
                        Hero = fallbackDeck.playerCard != null ? fallbackDeck.playerCard.cardName : ""
                    };
                    offlineDeck.Cards.AddRange(fallbackDeck.cards.Where(c => c != null).Select(c => c.cardName));
                }
                return offlineDeck;
            }
        }

        private void Start()
        {
            network = NetworkManager.Instance;

            // Pool und Helden-Liste immer aus dem Katalog ableiten — neue Karten erscheinen automatisch
            if (catalog != null)
            {
                cardPool = catalog.cards.Where(c => c != null && !(c is PlayerCardData) && !c.isToken).ToList();
                playerCards = catalog.cards.OfType<PlayerCardData>().ToList();
            }

            if (playerCardDropdown != null)
            {
                playerCardDropdown.ClearOptions();
                playerCardDropdown.AddOptions(playerCards.ConvertAll(p => p != null ? p.cardName : "?"));
                playerCardDropdown.onValueChanged.AddListener(OnPlayerCardChanged);
            }
            if (deckDropdown != null) deckDropdown.onValueChanged.AddListener(_ => OnDeckSelected());
            if (deckNameInput != null) deckNameInput.onEndEdit.AddListener(RenameDeck);
            if (saveButton != null) saveButton.onClick.AddListener(SaveCurrentDeck);
            if (newDeckButton != null) newDeckButton.onClick.AddListener(CreateDeck);
            if (deleteDeckButton != null) deleteDeckButton.onClick.AddListener(DeleteDeck);
            if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (network != null) network.OnMessage += HandleNet;

            if (feedbackText != null)
                feedbackText.text = CollectionMode
                    ? $"Logged in as {PlayerProfile.AccountName} — Save syncs changes to your account."
                    : "Not logged in — view-only mode with the starter deck.";

            RefreshDeckDropdown();
            Rebuild();
        }

        private void OnDestroy()
        {
            if (network != null) network.OnMessage -= HandleNet;
        }

        private void HandleNet(NetMessage message)
        {
            switch (message.t)
            {
                case "profile":
                case "auth_ok":
                    RefreshDeckDropdown();
                    Rebuild();
                    break;
                case "error":
                    ShowFeedback(message.msg);
                    break;
            }
        }

        private void RefreshDeckDropdown()
        {
            if (deckDropdown == null) return;
            int previous = deckDropdown.value;
            deckDropdown.ClearOptions();
            deckDropdown.AddOptions(CollectionMode
                ? PlayerProfile.Decks.ConvertAll(d => d.Name)
                : new List<string> { CurrentDeck != null ? CurrentDeck.Name : "—" });
            deckDropdown.SetValueWithoutNotify(Mathf.Clamp(previous, 0, Mathf.Max(0, deckDropdown.options.Count - 1)));

            bool online = CollectionMode;
            if (saveButton != null) saveButton.interactable = online;
            if (newDeckButton != null) newDeckButton.interactable = online;
            if (deleteDeckButton != null) deleteDeckButton.interactable = online && PlayerProfile.Decks.Count > 1;
            if (deckNameInput != null)
            {
                deckNameInput.interactable = online;
                deckNameInput.SetTextWithoutNotify(CurrentDeck != null ? CurrentDeck.Name : "");
            }
        }

        private void RenameDeck(string newName)
        {
            var deck = CurrentDeck;
            if (deck == null || !CollectionMode) return;
            string trimmed = (newName ?? "").Trim();
            if (trimmed.Length < 1 || trimmed.Length > 30)
            {
                ShowFeedback("Deck name must be 1–30 characters.");
                if (deckNameInput != null) deckNameInput.SetTextWithoutNotify(deck.Name);
                return;
            }
            if (trimmed == deck.Name) return;
            deck.Name = trimmed;
            RefreshDeckDropdown();
            ShowFeedback($"Deck renamed to {trimmed} (remember to save).");
        }

        private void OnDeckSelected()
        {
            var deck = CurrentDeck;
            if (deck != null && playerCardDropdown != null)
            {
                int index = playerCards.FindIndex(p => p != null && p.cardName == deck.Hero);
                if (index >= 0) playerCardDropdown.SetValueWithoutNotify(index);
            }
            Rebuild();
        }

        private void OnPlayerCardChanged(int index)
        {
            var deck = CurrentDeck;
            if (deck == null || index < 0 || index >= playerCards.Count) return;
            deck.Hero = playerCards[index] != null ? playerCards[index].cardName : "";
            ShowFeedback($"Player card: {deck.Hero} (remember to save)");
            Rebuild();
        }

        /// <summary>Alle Exemplare dieser Karte im Deck, gleich welches Finish.</summary>
        private int CountInDeck(CardDefinition card)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return 0;
            int count = 0;
            foreach (var name in deck.Cards) if (name == card.cardName) count++;
            return count;
        }

        /// <summary>Nur die Exemplare dieses einen Finishes.</summary>
        private int CountInDeck(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return 0;
            int count = 0;
            for (int i = 0; i < deck.Cards.Count; i++)
                if (deck.Cards[i] == card.cardName && deck.FinishAt(i) == finish) count++;
            return count;
        }

        /// <summary>
        /// Welche Exemplar-Typen einer Karte bekommen eine eigene Zeile? In der
        /// Sammlung alle, die man besitzt — sonst nur die schlichte.
        /// </summary>
        private System.Collections.Generic.List<CardFinish> RowsFor(CardDefinition card)
        {
            var result = new System.Collections.Generic.List<CardFinish>();
            if (!CollectionMode) { result.Add(CardFinish.Plain); return result; }

            var stock = PlayerProfile.StockOf(card.cardName);
            for (int i = 0; i < CardFinishInfo.Count; i++)
            {
                var finish = (CardFinish)i;
                // Schlicht steht immer da (auch mit 0 Stück — dort sitzen Craft/Dust),
                // besondere Exemplare nur, wenn man sie wirklich hat oder eingebaut hat.
                if (finish == CardFinish.Plain || stock[finish] > 0 || CountInDeck(card, finish) > 0)
                    result.Add(finish);
            }
            return result;
        }

        private static bool CanCraft(CardDefinition card) => PlayerProfile.Tokens(card.rarity) >= CraftCost;

        private void Rebuild()
        {
            if (poolContent != null) ClearChildren(poolContent);
            if (deckContent != null) ClearChildren(deckContent);
            var deck = CurrentDeck;
            if (deck == null || rowPrefab == null) { UpdateDeckInfo(); UpdateTokenBar(); return; }

            bool collection = CollectionMode;
            var sortedPool = cardPool.Where(c => c != null)
                .OrderBy(c => c.Kind)
                .ThenBy(c => c is MonsterCardData m ? m.level : 0)
                .ThenBy(c => c.cardName)
                .ToList();

            // Je Karte eine Zeile pro besessenem Finish — so lassen sich die zwei
            // Static gezielt einbauen statt der schlichten Exemplare.
            foreach (var card in sortedPool)
                foreach (var finish in RowsFor(card))
                {
                    var row = Instantiate(rowPrefab, poolContent);
                    row.Setup(card, finish, CountInDeck(card, finish), MaxCopies,
                        collection ? PlayerProfile.Owned(card.cardName, finish) : 1,
                        collection, CanCraft(card), CountInDeck(card),
                        AddCard, RemoveCard, HoverCard, CraftCard, DustCard);
                }

            foreach (var card in sortedPool)
                foreach (var finish in RowsFor(card))
                {
                    if (CountInDeck(card, finish) <= 0) continue;
                    var row = Instantiate(rowPrefab, deckContent);
                    row.Setup(card, finish, CountInDeck(card, finish), MaxCopies,
                        collection ? PlayerProfile.Owned(card.cardName, finish) : 1,
                        collection, CanCraft(card), CountInDeck(card),
                        AddCard, RemoveCard, HoverCard, CraftCard, DustCard);
                }

            UpdateDeckInfo();
            UpdateTokenBar();
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private bool DeckExceedsCollection()
        {
            if (!CollectionMode) return false;
            var deck = CurrentDeck;
            if (deck == null) return false;
            foreach (var name in deck.Cards)
                if (PlayerProfile.Owned(name) < 1) return true;
            if (!string.IsNullOrEmpty(deck.Hero) && PlayerProfile.Owned(deck.Hero) < 1) return true;
            return false;
        }

        private void UpdateDeckInfo()
        {
            var deck = CurrentDeck;
            if (deckInfoText == null) return;
            if (deck == null) { deckInfoText.text = "No deck."; return; }
            int count = deck.Cards.Count;
            bool valid = count >= DeckMin && count <= DeckMax;
            string color = valid ? "#7DDB6E" : "#E8695E";
            string hero = string.IsNullOrEmpty(deck.Hero) ? "—" : deck.Hero;
            string ownership = DeckExceedsCollection() ? "   <color=#E8695E>Contains cards you do not own!</color>" : "";
            deckInfoText.text = $"<color={color}>{count}</color> / {DeckMin}–{DeckMax} cards   •   Hero: {hero}{ownership}";
        }

        private void UpdateTokenBar()
        {
            if (tokensText == null) return;
            tokensText.gameObject.SetActive(CollectionMode);
            if (CollectionMode)
                tokensText.text = $"Tokens   <color=#CCCCD4>C {PlayerProfile.TokensCommon}</color>   " +
                                  $"<color=#73D973>U {PlayerProfile.TokensUncommon}</color>   " +
                                  $"<color=#66A6FF>R {PlayerProfile.TokensRare}</color>   " +
                                  $"<color=#FFB833>L {PlayerProfile.TokensLegendary}</color>" +
                                  $"   ·   <color=#F0C33C>{PlayerProfile.Coins} Coins</color>";
        }

        private void AddCard(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return;
            if (deck.Cards.Count >= DeckMax) { ShowFeedback($"Deck is full (max {DeckMax})."); return; }
            // Das Kopienlimit gilt für die Karte, nicht für das Exemplar
            if (CountInDeck(card) >= MaxCopies) { ShowFeedback($"Maximum {MaxCopies} copies per card."); return; }
            if (CollectionMode && CountInDeck(card, finish) >= PlayerProfile.Owned(card.cardName, finish))
            {
                ShowFeedback(finish == CardFinish.Plain
                    ? "You do not own another copy."
                    : $"You do not own another {CardFinishInfo.Label(finish)} copy.");
                return;
            }
            deck.Cards.Add(card.cardName);
            deck.CardFinishes.Add(finish);
            Rebuild();
        }

        private void RemoveCard(CardDefinition card, CardFinish finish)
        {
            var deck = CurrentDeck;
            if (deck == null || card == null) return;
            // Von hinten suchen: der zuletzt gelegte geht zuerst wieder raus
            for (int i = deck.Cards.Count - 1; i >= 0; i--)
            {
                if (deck.Cards[i] != card.cardName || deck.FinishAt(i) != finish) continue;
                deck.Cards.RemoveAt(i);
                if (i < deck.CardFinishes.Count) deck.CardFinishes.RemoveAt(i);
                Rebuild();
                return;
            }
        }

        private void HoverCard(CardDefinition card)
        {
            if (detailPanel != null && card != null) detailPanel.ShowDefinition(card);
        }

        private void CraftCard(CardDefinition card)
        {
            if (!CollectionMode || card == null) return;
            network.SendCraft(card.cardName);
            ShowFeedback($"Crafting {card.cardName} ({CraftCost} {CardDefinition.RarityName(card.rarity)} tokens)…");
        }

        private void DustCard(CardDefinition card)
        {
            if (!CollectionMode || card == null) return;
            network.SendDust(card.cardName);
            ShowFeedback($"Dusting {card.cardName} (+10 {CardDefinition.RarityName(card.rarity)} tokens)…");
        }

        private void SaveCurrentDeck()
        {
            var deck = CurrentDeck;
            if (!CollectionMode || deck == null) return;
            network.SendSaveDeck(CurrentIndex, deck);
            ShowFeedback($"Saving {deck.Name}…");
        }

        private void CreateDeck()
        {
            if (!CollectionMode) return;
            // Leer starten — so ist das Deck immer speicherbar, egal was man besitzt
            var fresh = new RuntimeDeck { Name = "Deck " + (PlayerProfile.Decks.Count + 1), Hero = "" };
            network.SendSaveDeck(PlayerProfile.Decks.Count, fresh);
            ShowFeedback($"Creating {fresh.Name} (empty)…");
        }

        private void DeleteDeck()
        {
            if (!CollectionMode || PlayerProfile.Decks.Count <= 1) return;
            network.SendDeleteDeck(CurrentIndex);
            ShowFeedback("Deleting deck…");
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
        }
    }
}
