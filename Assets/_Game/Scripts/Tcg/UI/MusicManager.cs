using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Hintergrundmusik über alle Szenen: Login-Theme, Menü-Loop und Duell-Tracks.
    /// Lebt als Singleton über Szenenwechsel hinweg (DontDestroyOnLoad) — dadurch läuft
    /// die Menümusik nahtlos weiter, solange man zwischen Menü-Szenen wechselt.
    /// Ein MusicManager liegt in jeder Szene, damit auch ein Direktstart (z.B. Duel
    /// im Editor) Musik hat; Duplikate zerstören sich selbst.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Tracks")]
        [Tooltip("Läuft im Login-/Titelscreen")]
        [SerializeField] private AudioClip loginMusic;

        [Tooltip("Läuft in allen Menü-Szenen (Hauptmenü, Shop, Deck-Editor, Duel-Setup)")]
        [SerializeField] private AudioClip menuMusic;

        [Tooltip("Läuft in der Münzwurf-Cutscene vor dem Duell")]
        [SerializeField] private AudioClip tossMusic;

        [Tooltip("Duell-Musik — pro Duell wird zufällig einer dieser Tracks gewählt")]
        [SerializeField] private AudioClip[] duelMusic;

        [Header("Szenen-Zuordnung")]
        [Tooltip("Szenen, in denen das Login-Theme läuft")]
        [SerializeField] private string[] loginScenes = { "Login" };

        [Tooltip("Szenen, in denen die Münzwurf-Musik läuft")]
        [SerializeField] private string[] tossScenes = new string[0];

        [Tooltip("Szenen, in denen die Duell-Musik läuft")]
        [SerializeField] private string[] duelScenes = { "Duel" };

        [Header("Klang")]
        [Range(0f, 1f)]
        [Tooltip("Grundlautstärke der Musik")]
        [SerializeField] private float volume = 0.4f;

        [Range(0.1f, 5f)]
        [Tooltip("Dauer des Überblendens bei Trackwechsel (Sekunden)")]
        [SerializeField] private float fadeDuration = 1.4f;

        private const string VolumePrefsKey = "rouge_music_volume";

        private AudioSource source;
        private Coroutine fadeRoutine;

        /// <summary>Aktuelle Musiklautstärke (0..1) — vom Settings-Menü gesteuert.</summary>
        public float MusicVolume => volume;

        /// <summary>Setzt und speichert die Musiklautstärke (Settings-Menü).</summary>
        public void SetMusicVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            if (fadeRoutine == null && source != null) source.volume = volume;
            PlayerPrefs.SetFloat(VolumePrefsKey, volume);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            volume = PlayerPrefs.GetFloat(VolumePrefsKey, volume);
            FillMissingClipsFromLibrary();

            source = gameObject.GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            ApplyForScene(scene.name);
        }

        private void ApplyForScene(string sceneName)
        {
            var clip = ClipForScene(sceneName);
            if (clip == null) return;                 // keine Zuordnung -> Musik weiterlaufen lassen
            if (source.clip == clip && source.isPlaying) return; // nahtlos (Menü -> Menü)
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(CrossfadeTo(clip));
        }

        /// <summary>
        /// Nicht im Inspector gesetzte Tracks kommen aus der Audio-Bibliothek. Dadurch
        /// hat auch eine Szene Musik, in der der MusicManager nie verdrahtet wurde.
        /// </summary>
        private void FillMissingClipsFromLibrary()
        {
            var library = AudioLibrary.Load();
            if (library == null) return;
            if (loginMusic == null) loginMusic = library.loginMusic;
            if (menuMusic == null) menuMusic = library.menuMusic;
            if (tossMusic == null) tossMusic = library.tossMusic;
            if (duelMusic == null || duelMusic.Length == 0) duelMusic = library.duelMusic;
        }

        private AudioClip ClipForScene(string sceneName)
        {
            foreach (var s in loginScenes)
                if (sceneName == s) return loginMusic;
            foreach (var s in tossScenes)
                if (sceneName == s) return tossMusic;
            foreach (var s in duelScenes)
                if (sceneName == s) return PickDuelTrack();
            return menuMusic;
        }

        private AudioClip PickDuelTrack()
        {
            if (duelMusic == null || duelMusic.Length == 0) return null;
            return duelMusic[Random.Range(0, duelMusic.Length)];
        }

        private IEnumerator CrossfadeTo(AudioClip clip)
        {
            float half = fadeDuration * 0.5f;
            if (source.isPlaying && source.clip != null)
            {
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(volume, 0f, t / half);
                    yield return null;
                }
            }
            source.clip = clip;
            source.volume = 0f;
            source.Play();
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, volume, t / half);
                yield return null;
            }
            source.volume = volume;
            fadeRoutine = null;
        }
    }
}
