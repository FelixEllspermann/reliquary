using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rouge.Tcg.Net;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Login-Screen (Shell-Design "Reliquary"): Modus-Tabs, Passwort-Reveal, Remember-Siegel,
    /// Busy-Sweep, Fehler-Shake und Status-Zeile. Netzwerk-Fluss wie gehabt: Login/Registrieren
    /// über den NetworkManager, danach Hauptmenü; Offline-Weiter jederzeit.
    /// </summary>
    public class LoginController : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private NetworkManager network;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField passInput;
        [SerializeField] private Button loginButton;      // primäre CTA
        [SerializeField] private Button registerButton;   // (alt, ungenutzt — Tabs übernehmen)
        [SerializeField] private TMP_Text statusText;

        [Header("Shell-Design")]
        [SerializeField] private Button signInTabButton;
        [SerializeField] private Button registerTabButton;
        [SerializeField] private Image signInTabBg;
        [SerializeField] private Image registerTabBg;
        [SerializeField] private TMP_Text signInTabLabel;
        [SerializeField] private TMP_Text registerTabLabel;
        [SerializeField] private TMP_Text primaryLabel;    // LOG IN / CREATE ACCOUNT / UNSEALING…
        [SerializeField] private RectTransform busySweep;  // Sweep-Band im CTA
        [SerializeField] private Button showPasswordButton;
        [SerializeField] private TMP_Text showPasswordLabel;
        [SerializeField] private Button rememberButton;
        [SerializeField] private GameObject rememberCore;  // Gold-Kern des Diamant-Checkbox
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Image nameFieldBorder;
        [SerializeField] private Image passFieldBorder;
        [SerializeField] private Image statusDot;
        [SerializeField] private RectTransform authPanel;  // für den Fehler-Shake
        [SerializeField] private CanvasGroup formGroup;    // Busy: 60% + gesperrt
        [SerializeField] private CardSkin skin;

        [Header("Steam (zweiter Anmeldeweg)")]
        [SerializeField] private Button steamButton;
        [SerializeField] private TMP_Text steamLabel;
        [SerializeField] private GameObject steamDivider;

        [Header("Einstellungen")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string starterPickSceneName = "StarterPick";

        private const string RememberPrefKey = "rouge_remember_name";

        private bool registerMode;
        private bool busy;
        private bool showPassword;
        private bool remember;
        private Coroutine sweepRoutine;
        private Coroutine shakeRoutine;

        private static readonly Color BorderNormal = new Color(0.784f, 0.643f, 0.361f, 0.4f);  // rgba(200,164,92,.4)
        private static readonly Color BorderError = new Color(0.878f, 0.376f, 0.227f, 1f);     // #E0603A
        private static readonly Color DotOnline = new Color(0.478f, 0.804f, 0.588f, 1f);       // #7ACD96
        private static readonly Color DotOffline = new Color(0.788f, 0.478f, 0.361f, 1f);      // #C97A5C

        private void Start()
        {
            if (loginButton != null) loginButton.onClick.AddListener(() => Authenticate(registerMode));
            if (registerButton != null) registerButton.onClick.AddListener(() => Authenticate(true));
            if (signInTabButton != null) signInTabButton.onClick.AddListener(() => SetMode(false));
            if (registerTabButton != null) registerTabButton.onClick.AddListener(() => SetMode(true));
            if (showPasswordButton != null) showPasswordButton.onClick.AddListener(TogglePassword);
            if (rememberButton != null) rememberButton.onClick.AddListener(ToggleRemember);
            if (steamButton != null) steamButton.onClick.AddListener(AuthenticateWithSteam);
            SetupSteam();

            // Tastenanschlag hörbar machen — nur beim Wachsen des Textes, nicht beim Löschen
            HookTypeSound(nameInput);
            HookTypeSound(passInput);

            // Remember-Siegel: gespeicherten Namen vorbefüllen
            string savedName = PlayerPrefs.GetString(RememberPrefKey, "");
            remember = !string.IsNullOrEmpty(savedName);
            if (remember && nameInput != null) nameInput.text = savedName;
            if (rememberCore != null) rememberCore.SetActive(remember);

            SetMode(false);
            SetAuthEnabled(false);
            ClearError();

            if (PlayerProfile.LoggedIn && network != null && network.IsConnected)
            {
                // Auch dieser Abkürzungsweg (schon angemeldet, Login übersprungen)
                // darf die Startdeck-Wahl nicht überspringen.
                SceneManager.LoadScene(PlayerProfile.StarterPending && PlayerProfile.StarterDecks.Count > 0
                    ? starterPickSceneName : mainMenuSceneName);
                return;
            }

            if (network != null)
            {
                network.OnConnected += HandleConnected;
                network.OnDisconnected += HandleDisconnected;
                network.OnMessage += HandleMessage;
                SetStatus(false, Loc.T("Reaching for the vault…"));
                network.Connect();
            }
            else SetStatus(false, Loc.T("Vault unreachable — retrying may help"));
        }

        private void OnDestroy()
        {
            if (network == null) return;
            network.OnConnected -= HandleConnected;
            network.OnDisconnected -= HandleDisconnected;
            network.OnMessage -= HandleMessage;
        }

        private void Update()
        {
            // Enter schickt das Formular ab
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && !busy &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                if (loginButton != null && loginButton.interactable) Authenticate(registerMode);
            }
        }

        /// <summary>Spielt beim Tippen einen Anschlag — auch beim Löschen, aber nur einmal pro Zeichen.</summary>
        private static void HookTypeSound(TMP_InputField field)
        {
            if (field == null) return;
            int previousLength = field.text != null ? field.text.Length : 0;
            field.onValueChanged.AddListener(value =>
            {
                int length = value != null ? value.Length : 0;
                if (length != previousLength) SfxManager.Type();
                previousLength = length;
            });
        }

        // ---------- Modus-Tabs ----------
        private void SetMode(bool register)
        {
            registerMode = register;
            ClearError();
            ApplyTabVisual(signInTabBg, signInTabLabel, !register);
            ApplyTabVisual(registerTabBg, registerTabLabel, register);
            if (primaryLabel != null && !busy) primaryLabel.text = register ? Loc.T("CREATE ACCOUNT") : Loc.T("LOG IN");
        }

        private void ApplyTabVisual(Image bg, TMP_Text label, bool active)
        {
            if (bg != null)
            {
                bg.sprite = active && skin != null ? skin.badgeMonster : null;
                bg.color = active ? Color.white : Color.clear;
            }
            if (label != null)
                label.color = active ? new Color32(0x1E, 0x14, 0x05, 0xFF) : new Color32(0x8C, 0x7B, 0x5F, 0xFF);
        }

        // ---------- Passwort & Remember ----------
        private void TogglePassword()
        {
            showPassword = !showPassword;
            if (passInput != null)
            {
                passInput.contentType = showPassword ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
                passInput.ForceLabelUpdate();
            }
            if (showPasswordLabel != null) showPasswordLabel.text = showPassword ? Loc.T("HIDE") : Loc.T("SHOW");
        }

        private void ToggleRemember()
        {
            remember = !remember;
            if (rememberCore != null) rememberCore.SetActive(remember);
            if (!remember) PlayerPrefs.DeleteKey(RememberPrefKey);
        }

        // ---------- Status & Fehler ----------
        private void SetAuthEnabled(bool enabled)
        {
            if (loginButton != null) loginButton.interactable = enabled && !busy;
        }

        private void SetStatus(bool online, string text)
        {
            if (statusText != null) statusText.text = text;
            if (statusDot != null) statusDot.color = online ? DotOnline : DotOffline;
        }

        private void ShowError(string message)
        {
            if (errorText != null) { errorText.text = message; errorText.gameObject.SetActive(true); }
            if (nameFieldBorder != null) nameFieldBorder.color = BorderError;
            if (passFieldBorder != null) passFieldBorder.color = BorderError;
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            if (authPanel != null) shakeRoutine = StartCoroutine(Shake());
        }

        private void ClearError()
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
            if (nameFieldBorder != null) nameFieldBorder.color = BorderNormal;
            if (passFieldBorder != null) passFieldBorder.color = BorderNormal;
        }

        private IEnumerator Shake()
        {
            Vector2 basePos = authPanel.anchoredPosition;
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                float x = Mathf.Sin(elapsed * 90f) * 6f * damper;
                authPanel.anchoredPosition = basePos + new Vector2(x, 0f);
                yield return null;
            }
            authPanel.anchoredPosition = basePos;
        }

        // ---------- Busy ----------
        private void SetBusy(bool value)
        {
            busy = value;
            if (primaryLabel != null) primaryLabel.text = value ? Loc.T("UNSEALING…") : (registerMode ? Loc.T("CREATE ACCOUNT") : Loc.T("LOG IN"));
            if (formGroup != null)
            {
                formGroup.interactable = !value;
                formGroup.alpha = value ? 0.6f : 1f;
            }
            if (busySweep != null)
            {
                busySweep.gameObject.SetActive(value);
                if (sweepRoutine != null) StopCoroutine(sweepRoutine);
                if (value) sweepRoutine = StartCoroutine(SweepLoop());
            }
            SetAuthEnabled(network != null && network.IsConnected);
        }

        private IEnumerator SweepLoop()
        {
            var button = (RectTransform)busySweep.parent;
            while (busy)
            {
                float width = button.rect.width;
                float bandWidth = width * 0.34f;
                busySweep.sizeDelta = new Vector2(bandWidth, busySweep.sizeDelta.y);
                float elapsed = 0f;
                const float duration = 1f;
                while (elapsed < duration && busy)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float k = elapsed / duration;
                    busySweep.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.6f, width * 0.6f + bandWidth, k), 0f);
                    yield return null;
                }
            }
        }

        // ---------- Netzwerk ----------
        private Coroutine retryRoutine;

        private void HandleConnected()
        {
            if (retryRoutine != null) { StopCoroutine(retryRoutine); retryRoutine = null; }
            SetAuthEnabled(true);
            SetStatus(true, Loc.T("Connected to the vault"));
        }

        private void HandleDisconnected(string reason)
        {
            SetBusy(false);
            SetAuthEnabled(false);
            SetStatus(false, Loc.T("Vault unreachable — retrying…"));
            if (retryRoutine == null) retryRoutine = StartCoroutine(RetryConnect());
        }

        /// <summary>Ohne Offline-Modus ist der Login der einzige Weg — also automatisch neu verbinden.</summary>
        private IEnumerator RetryConnect()
        {
            while (network != null && !network.IsConnected)
            {
                yield return new WaitForSecondsRealtime(5f);
                if (network == null || network.IsConnected) break;
                SetStatus(false, Loc.T("Reaching for the vault…"));
                network.Connect();
                yield return new WaitForSecondsRealtime(2f);
                if (network != null && !network.IsConnected) SetStatus(false, Loc.T("Vault unreachable — retrying…"));
            }
            retryRoutine = null;
        }

        private void Authenticate(bool register)
        {
            if (busy) return;
            if (network == null || !network.IsConnected) { ShowError(Loc.T("Not connected to the vault.")); return; }
            string name = nameInput != null ? nameInput.text.Trim() : "";
            string pass = passInput != null ? passInput.text : "";
            if (name.Length < 3) { ShowError(Loc.T("Name too short (min. 3 characters).")); return; }
            if (pass.Length < 4) { ShowError(Loc.T("Password too short (min. 4 characters).")); return; }
            ClearError();
            SetBusy(true);
            if (register) network.SendRegister(name, pass);
            else network.SendLogin(name, pass);
        }

        // ---------- Steam ----------

        /// <summary>
        /// Der Steam-Weg erscheint nur, wenn Steam wirklich läuft. In einer
        /// Version ohne Steam-SDK (oder ausserhalb von Steam gestartet) bleibt
        /// die Zeile komplett verborgen — kein toter Knopf.
        /// </summary>
        private void SetupSteam()
        {
            bool available = SteamBridge.Available;
            if (steamDivider != null) steamDivider.SetActive(available);
            if (steamButton != null) steamButton.gameObject.SetActive(available);
            if (available && steamLabel != null)
            {
                steamLabel.text = string.IsNullOrEmpty(SteamBridge.PersonaName)
                    ? Loc.T("CONTINUE WITH STEAM")
                    : Loc.F("CONTINUE AS {0}", SteamBridge.PersonaName.ToUpperInvariant());
            }
        }

        private void AuthenticateWithSteam()
        {
            if (busy) return;
            if (network == null || !network.IsConnected) { ShowError(Loc.T("Not connected to the vault.")); return; }
            ClearError();
            SetBusy(true);
            // Das Ticket kommt asynchron — erst wenn Steam es freigegeben hat,
            // akzeptiert Valve es auch (siehe SteamBridge.RequestAuthTicket).
            SteamBridge.RequestAuthTicket(
                ticket => network.SendSteamAuth(ticket, SteamBridge.PersonaName),
                reason => { SetBusy(false); ShowError(reason); });
        }

        /// <summary>
        /// Der Siegel-Übergang deckt den Szenenwechsel ab: er lädt das Hauptmenü
        /// im Weisspunkt und gibt sich erst frei, wenn die Szene wirklich steht.
        /// </summary>
        private void EnterVault()
        {
            // Wer noch kein Startdeck hat, wählt zuerst. Der Tresor-Übergang würde
            // hier nur eine Zahl feiern, die noch bei null steht.
            if (PlayerProfile.StarterPending && PlayerProfile.StarterDecks.Count > 0)
            {
                SceneManager.LoadScene(starterPickSceneName);
                return;
            }

            int cards = 0;
            foreach (var count in PlayerProfile.Collection.Values) cards += count;
            string vaultLine = PlayerProfile.Decks.Count == 1
                ? Loc.F("Your vault holds {0} cards and {1} deck.", cards, PlayerProfile.Decks.Count)
                : Loc.F("Your vault holds {0} cards and {1} decks.", cards, PlayerProfile.Decks.Count);

            var transition = VaultEnterTransition.Play(
                PlayerProfile.AccountName, vaultLine, PlayerProfile.OnlineCount);

            AsyncOperation load = null;
            transition.OnCurtainPeak += () =>
            {
                load = SceneManager.LoadSceneAsync(mainMenuSceneName);
                load.allowSceneActivation = true;
            };
            transition.OnArriveSettled += () =>
            {
                if (load == null || load.isDone) transition.ReleaseToMenu();
                else load.completed += _ => transition.ReleaseToMenu();
            };
        }

        private void HandleMessage(NetMessage message)
        {
            switch (message.t)
            {
                case "auth_ok":
                    // Der Ton meldet eine wartende Belohnung — nicht jeden Login.
                    // PlayerProfile ist hier schon aktualisiert (NetworkManager wendet das
                    // Profil an, bevor er das Ereignis auslöst).
                    if (PlayerProfile.DailyClaimable) SfxManager.Claim();
                    // Bei der Steam-Anmeldung gibt es kein Namensfeld zum Merken
                    if (remember && nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
                        PlayerPrefs.SetString(RememberPrefKey, nameInput.text.Trim());
                    else PlayerPrefs.DeleteKey(RememberPrefKey);
                    PlayerPrefs.Save();
                    EnterVault();
                    break;
                case "error":
                    SetBusy(false);
                    ShowError(message.msg);
                    break;
            }
        }
    }
}
