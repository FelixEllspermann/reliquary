using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>Online-Play: Account-Deck wählen, Quick Match oder Lobby-Code, dann ins Duell.</summary>
    public class PlayController : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private TMP_Dropdown deckDropdown;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button quickMatchButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Einstellungen")]
        [SerializeField] private string duelSceneName = "Duel";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField, Tooltip("Regelwerk für die Deckgrößen-Prüfung")] private GameRules rules;

        private NetworkManager network;
        private bool loadingMatch;

        private RuntimeDeck SelectedDeck =>
            PlayerProfile.Decks.Count == 0 ? null
            : PlayerProfile.Decks[Mathf.Clamp(deckDropdown != null ? deckDropdown.value : 0, 0, PlayerProfile.Decks.Count - 1)];

        private void Start()
        {
            network = NetworkManager.Instance;
            if (deckDropdown != null)
            {
                deckDropdown.ClearOptions();
                deckDropdown.AddOptions(PlayerProfile.Decks.ConvertAll(d => d.Name));
                // Zuletzt gespieltes Deck vorwählen — ein Schlüssel für Play, Solo und Builder
                deckDropdown.SetValueWithoutNotify(Mathf.Clamp(
                    PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, 0),
                    0, Mathf.Max(0, PlayerProfile.Decks.Count - 1)));
                deckDropdown.RefreshShownValue();
                deckDropdown.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(MainMenuController.ActiveDeckPrefKey, v);
                    PlayerPrefs.Save();
                });
            }
            if (quickMatchButton != null) quickMatchButton.onClick.AddListener(QuickMatch);
            if (createButton != null) createButton.onClick.AddListener(CreateLobby);
            if (joinButton != null) joinButton.onClick.AddListener(JoinLobby);
            if (backButton != null) backButton.onClick.AddListener(Back);

            if (network == null || !network.IsConnected || !PlayerProfile.LoggedIn)
            {
                SetStatus("Not logged in — go back to the main menu.");
                SetButtons(false);
                return;
            }
            network.OnMessage += HandleMessage;
            network.OnDisconnected += HandleDisconnected;
            SetStatus("Choose your deck and a game mode.");
        }

        private void OnDestroy()
        {
            if (network == null) return;
            network.OnMessage -= HandleMessage;
            network.OnDisconnected -= HandleDisconnected;
        }

        private void SetButtons(bool active)
        {
            if (quickMatchButton != null) quickMatchButton.interactable = active;
            if (createButton != null) createButton.interactable = active;
            if (joinButton != null) joinButton.interactable = active;
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void HandleDisconnected(string reason) { SetButtons(false); SetStatus(reason); }

        private void Back()
        {
            if (network != null) network.SendLeave();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private bool Prepare()
        {
            var deck = SelectedDeck;
            if (deck == null) { SetStatus("No deck available — build one in the Decks screen!"); return false; }
            int deckMin = rules != null ? rules.deckMinSize : 40;
            int deckMax = rules != null ? rules.deckMaxSize : 80;
            if (deck.Cards.Count < deckMin || deck.Cards.Count > deckMax)
            {
                SetStatus($"\"{deck.Name}\" has {deck.Cards.Count} cards — allowed: {deckMin}–{deckMax}.");
                return false;
            }
            if (!DeckIsOwned(deck))
            {
                SetStatus("Your deck contains cards you do not own — please adjust it in the Decks screen.");
                return false;
            }
            network.SendHello(PlayerProfile.AccountName);
            return true;
        }

        private static bool DeckIsOwned(RuntimeDeck deck)
        {
            foreach (var cardName in deck.Cards)
                if (PlayerProfile.Owned(cardName) < 1) return false;
            if (!string.IsNullOrEmpty(deck.Hero) && PlayerProfile.Owned(deck.Hero) < 1) return false;
            return true;
        }

        private void QuickMatch()
        {
            if (!Prepare()) return;
            int deckIndex = deckDropdown != null ? deckDropdown.value : 0;
            network.SendQueue(deckIndex);
            SetStatus("Searching for an opponent…");
        }

        private void CreateLobby()
        {
            if (!Prepare()) return;
            network.SendCreate();
            SetStatus("Creating lobby…");
        }

        private void JoinLobby()
        {
            if (!Prepare()) return;
            string code = codeInput != null ? codeInput.text.Trim().ToUpperInvariant() : "";
            if (code.Length != 4) { SetStatus("Please enter the 4-letter lobby code."); return; }
            network.SendJoin(code);
            SetStatus($"Joining lobby {code}…");
        }

        private void HandleMessage(NetMessage message)
        {
            switch (message.t)
            {
                case "queued":
                    SetStatus("In queue — waiting for an opponent…");
                    break;
                case "lobby":
                    SetStatus($"Lobby code: {message.code} — share it with your friend!");
                    break;
                case "error":
                    SetStatus("Error: " + message.msg);
                    break;
                case "peer_left":
                    if (!loadingMatch) SetStatus("The opponent disconnected.");
                    break;
                case "sduel_start":
                    // Server-autoritatives Duell: kein Deck-Tausch, der Server kennt beide Decks
                    if (loadingMatch) break;
                    loadingMatch = true;
                    MatchContext.Clear();
                    MatchContext.IsServerMatch = true;
                    MatchContext.LocalIsPlayerA = message.youAre == "A";
                    MatchContext.LocalName = PlayerProfile.AccountName;
                    MatchContext.RemoteName = string.IsNullOrEmpty(message.opponent) ? "Opponent" : message.opponent;
                    MatchContext.SetRemoteCosmetics(message.oppSlots, message.oppIds);
                    SetStatus("Match starting!");
                    SceneManager.LoadScene(duelSceneName);
                    break;
            }
        }

    }
}
