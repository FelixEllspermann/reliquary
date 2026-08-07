using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>Solo gegen den Bot — mit einem Account-Deck oder dem Standard-Starter.</summary>
    public class SoloController : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private TMP_Dropdown deckDropdown;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Einstellungen")]
        [SerializeField] private string duelSceneName = "Duel";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField, Tooltip("Regelwerk für die Deckgrößen-Prüfung")] private GameRules rules;

        private void Start()
        {
            bool hasAccountDecks = PlayerProfile.LoggedIn && PlayerProfile.Decks.Count > 0;
            if (deckDropdown != null)
            {
                deckDropdown.ClearOptions();
                deckDropdown.AddOptions(hasAccountDecks
                    ? PlayerProfile.Decks.ConvertAll(d => d.Name)
                    : new List<string> { "Pyro Starter (default)" });
                if (hasAccountDecks)
                {
                    // Zuletzt gespieltes Deck vorwählen — gleicher Schlüssel wie Play/Builder
                    deckDropdown.SetValueWithoutNotify(Mathf.Clamp(
                        PlayerPrefs.GetInt(MainMenuController.ActiveDeckPrefKey, 0),
                        0, PlayerProfile.Decks.Count - 1));
                    deckDropdown.RefreshShownValue();
                    deckDropdown.onValueChanged.AddListener(v =>
                    {
                        PlayerPrefs.SetInt(MainMenuController.ActiveDeckPrefKey, v);
                        PlayerPrefs.Save();
                    });
                }
            }
            if (startButton != null) startButton.onClick.AddListener(StartSolo);
            if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
            if (statusText != null)
                statusText.text = hasAccountDecks
                    ? "Choose your deck — the bot plays Frost & Light."
                    : "Without an account you play the default starter deck.";
        }

        private void StartSolo()
        {
            MatchContext.Clear();
            if (PlayerProfile.LoggedIn && PlayerProfile.Decks.Count > 0)
            {
                var deck = PlayerProfile.Decks[Mathf.Clamp(deckDropdown != null ? deckDropdown.value : 0, 0, PlayerProfile.Decks.Count - 1)];
                int deckMin = rules != null ? rules.deckMinSize : 40;
                int deckMax = rules != null ? rules.deckMaxSize : 80;
                if (deck.Cards.Count < deckMin || deck.Cards.Count > deckMax)
                {
                    if (statusText != null)
                        statusText.text = $"\"{deck.Name}\" has {deck.Cards.Count} cards — allowed: {deckMin}–{deckMax}.";
                    return;
                }
                MatchContext.UseCustomLocalDeck = true;
                MatchContext.LocalDeckCards = new List<string>(deck.Cards);
                MatchContext.LocalExtraCards = new List<string>(deck.Extra);
                MatchContext.LocalDeckFinishes = deck.DeckFinishNumbers();
                MatchContext.LocalExtraFinishes = deck.ExtraFinishNumbers();
                MatchContext.LocalHero = deck.Hero;
                MatchContext.LocalName = PlayerProfile.AccountName;
            }
            SceneManager.LoadScene(duelSceneName);
        }
    }
}
