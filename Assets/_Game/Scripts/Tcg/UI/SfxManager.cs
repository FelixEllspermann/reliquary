using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Kurze Oberflächen-Sounds: Hover, Klick, Tippen, Belohnung. Erzeugt sich beim
    /// Spielstart selbst und lebt über alle Szenen hinweg — es muss nichts in Szenen
    /// verdrahtet werden. Mehrere Stimmen, damit sich schnelle Klicks nicht abschneiden.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        private const string VolumePrefsKey = "rouge_sfx_volume";
        private const int VoiceCount = 6;

        /// <summary>Mindestabstand zwischen zwei Hover-Tönen — sonst rattert es beim Überfahren.</summary>
        private const float HoverCooldown = 0.05f;

        public static SfxManager Instance { get; private set; }

        private AudioLibrary library;
        private AudioSource[] voices;
        private AudioListener ownListener;
        private int nextVoice;
        private float volume = 0.7f;
        private float lastHover = -10f;

        /// <summary>Aktuelle Effektlautstärke (0..1) — vom Settings-Menü gesteuert.</summary>
        public float SfxVolume => volume;

        /// <summary>Setzt und speichert die Effektlautstärke.</summary>
        public void SetSfxVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumePrefsKey, volume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("~Sfx").AddComponent<SfxManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            volume = PlayerPrefs.GetFloat(VolumePrefsKey, volume);
            library = AudioLibrary.Load();

            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                voices[i] = source;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureListener();
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureListener();

        /// <summary>
        /// Ohne AudioListener bleibt eine Szene stumm, mit zweien warnt Unity in jedem
        /// Frame. Deshalb wird hier ausdrücklich nach einem FREMDEN Listener gesucht —
        /// die frühere Prüfung fand den eigenen und ließ ihn neben dem der Kamera stehen.
        /// </summary>
        private void EnsureListener()
        {
            bool foreignListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener == ownListener) continue;
                foreignListener = true;
                break;
            }

            if (foreignListener)
            {
                if (ownListener == null) return;
                ownListener.enabled = false;   // Destroy greift erst am Frame-Ende — sofort stillegen
                Destroy(ownListener);
                ownListener = null;
            }
            else if (ownListener == null)
            {
                ownListener = gameObject.AddComponent<AudioListener>();
            }
        }

        // ================== ABSPIELEN ==================

        /// <summary>
        /// Lautstärke des Hover-Tons auf gewöhnlichen Knöpfen, relativ zu den großen
        /// Menü-Kacheln — die dürfen lauter bleiben, weil man sie seltener überfährt.
        /// </summary>
        public const float ButtonHoverGain = 0.7f;

        /// <summary>Button/Kachel überfahren. <paramref name="volumeScale"/> dämpft den Ton.</summary>
        public static void Hover(float volumeScale = 1f)
        {
            var self = Instance;
            if (self == null) return;
            if (Time.unscaledTime - self.lastHover < HoverCooldown) return;
            self.lastHover = Time.unscaledTime;
            self.PlayClip(self.library != null ? self.library.hover : null, 0.5f * volumeScale, 0.04f);
        }

        /// <summary>Button gedrückt.</summary>
        public static void Click() => Play(clip => clip.buttonPress, 0.9f, 0.03f);

        /// <summary>Tastenanschlag in einem Eingabefeld.</summary>
        public static void Type() => Play(clip => clip.type, 0.45f, 0.06f);

        /// <summary>Belohnung/Bestätigung.</summary>
        public static void Claim() => Play(clip => clip.claim, 1f, 0f);

        // ---------- Duell ----------

        /// <summary>Karte überfahren — teilt sich die Sperre mit dem Button-Hover, damit nie beides gleichzeitig klingt.</summary>
        public static void CardHover()
        {
            var self = Instance;
            if (self == null) return;
            if (Time.unscaledTime - self.lastHover < HoverCooldown) return;
            self.lastHover = Time.unscaledTime;
            self.PlayClip(self.library != null ? self.library.cardHover : null, 0.85f, 0.05f);
        }

        /// <summary>Karte wird gezogen.</summary>
        public static void CardDraw() => Play(clip => clip.cardDraw, 1.25f, 0.04f);

        /// <summary>Karte landet auf dem Feld.</summary>
        public static void CardPlace() => Play(clip => clip.cardPlace, 1.35f, 0.04f);

        /// <summary>Karteneffekt wird aktiviert — der lauteste Ton im Duell.</summary>
        public static void CardActivate() => Play(clip => clip.cardActivate, 1.5f, 0.03f);

        /// <summary>Angriff trifft eine Karte oder den Spieler.</summary>
        public static void Hit() => Play(clip => clip.hit, 0.95f, 0.05f);

        /// <summary>Karte wird zerstört.</summary>
        public static void Destroyed() => Play(clip => clip.destroyed, 0.9f, 0.04f);

        /// <summary>Karte fliegt in Friedhof oder Verbannung.</summary>
        public static void CardMoving() => Play(clip => clip.cardMoving, 0.6f, 0.05f);

        // ---------- Münzwurf ----------

        /// <summary>Die Münze wird geworfen.</summary>
        public static void CoinToss() => Play(clip => clip.coinToss, 1f, 0f);

        /// <summary>Eine volle Umdrehung der Münze.</summary>
        public static void CoinTurn() => Play(clip => clip.coinTurn, 0.55f, 0.07f);

        /// <summary>Die Münze schlägt auf.</summary>
        public static void CoinHit() => Play(clip => clip.coinHit, 1f, 0.03f);

        // ---------- Übergänge ----------

        /// <summary>Ein Schloss des Siegels löst aus (sechsmal im Login-Übergang).</summary>
        public static void SealUnlock() => Play(clip => clip.sealUnlock, 0.8f, 0.05f);

        /// <summary>Das Siegel bricht auf.</summary>
        public static void SealOpen() => Play(clip => clip.sealOpen, 1f, 0f);

        /// <summary>Das Deck wird gemischt.</summary>
        public static void CardShuffle() => Play(clip => clip.cardShuffle, 0.85f, 0.03f);

        private static void Play(System.Func<AudioLibrary, AudioClip> pick, float gain, float jitter)
        {
            var self = Instance;
            if (self == null || self.library == null) return;
            self.PlayClip(pick(self.library), gain, jitter);
        }

        private void PlayClip(AudioClip clip, float gain, float pitchJitter)
        {
            if (clip == null || volume <= 0.001f || voices == null) return;
            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * gain);
            source.pitch = pitchJitter > 0f ? 1f + Random.Range(-pitchJitter, pitchJitter) : 1f;
            source.Play();
        }
    }
}
