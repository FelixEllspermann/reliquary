using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Bildschirmrütteln für Treffer und schwere Momente. Läuft als eigener
    /// DontDestroyOnLoad-Dienst und verschiebt die aktive Kamera — dadurch wackelt
    /// auch die UI mit, solange sie an dieser Kamera hängt, und Szenenwechsel
    /// unterbrechen ihn nicht.
    ///
    /// Die Stärke ist eine Einstellung (0 = aus). Sie liegt in den PlayerPrefs, damit
    /// sie den Neustart überlebt, und wird über <see cref="Strength"/> gelesen.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        private const string PrefKey = "rouge_shake_strength";
        private const float DefaultStrength = 1f;

        public static ScreenShake Instance { get; private set; }

        private static float strength = -1f;

        /// <summary>Nutzer-Stärke, 0 = aus bis 1.5 = kräftig.</summary>
        public static float Strength
        {
            get
            {
                if (strength < 0f) strength = PlayerPrefs.GetFloat(PrefKey, DefaultStrength);
                return strength;
            }
        }

        public static void SetStrength(float value)
        {
            strength = Mathf.Clamp(value, 0f, 1.5f);
            PlayerPrefs.SetFloat(PrefKey, strength);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("~ScreenShake");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ScreenShake>();
        }

        // ---- laufender Stoß ----
        private float amplitude;      // in Weltmetern der Kamera
        private float duration;
        private float elapsed;
        private float frequency = 26f;
        private Vector2 seed;

        private Transform target;
        private Vector3 restPosition;
        private bool holding;

        /// <summary>
        /// Ein Stoß. <paramref name="power"/> ist in „Bildschirmanteilen" gedacht:
        /// 0.006 ist ein leichtes Ticken, 0.02 ein satter Treffer.
        /// </summary>
        public static void Shake(float power, float seconds = 0.28f, float hz = 26f)
        {
            var self = Instance;
            if (self == null || Strength <= 0.001f) return;
            self.Begin(power * Strength, seconds, hz);
        }

        /// <summary>Ein Kartenangriff trifft.</summary>
        public static void Impact() => Shake(0.014f, 0.26f);

        /// <summary>Der Spieler selbst nimmt Schaden — spürbar stärker.</summary>
        public static void HeavyImpact() => Shake(0.024f, 0.38f, 22f);

        /// <summary>Leichtes Ticken, z.B. wenn ein Schloss auslöst.</summary>
        public static void Tick() => Shake(0.005f, 0.18f, 32f);

        private void Begin(float power, float seconds, float hz)
        {
            // Ein stärkerer Stoß überschreibt einen schwächeren, statt sich zu addieren
            if (holding && power < amplitude * (1f - elapsed / Mathf.Max(duration, 0.0001f))) return;
            amplitude = power;
            duration = Mathf.Max(0.02f, seconds);
            frequency = hz;
            elapsed = 0f;
            seed = new Vector2(Random.value * 97f, Random.value * 63f);
            EnsureTarget();
        }

        /// <summary>
        /// Wohin der Stoß geht. Ein Overlay-Canvas sieht die Kamera NICHT — wer dort
        /// nur die Kamera schüttelt, schüttelt nichts Sichtbares. Deshalb meldet
        /// die Oberfläche ihre eigene Wurzel an, und nur wenn keine da ist,
        /// fällt es auf die Kamera zurück.
        /// </summary>
        private static Transform uiTarget;

        /// <summary>Meldet die UI-Wurzel an, die geschüttelt werden soll.</summary>
        public static void SetUiTarget(Transform root)
        {
            if (Instance != null && Instance.target != null && Instance.target != root)
                Instance.ReleaseTarget();
            uiTarget = root;
        }

        private void EnsureTarget()
        {
            var wanted = uiTarget != null ? uiTarget
                : Camera.main != null ? Camera.main.transform : null;
            if (wanted == null) { holding = false; return; }
            if (target != wanted)
            {
                ReleaseTarget();
                target = wanted;
                restPosition = target.localPosition;
            }
            holding = true;
        }

        private void ReleaseTarget()
        {
            if (target != null && holding) target.localPosition = restPosition;
            holding = false;
        }

        private void LateUpdate()
        {
            if (!holding) return;
            if (target == null) { holding = false; return; }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration)
            {
                target.localPosition = restPosition;
                holding = false;
                return;
            }

            // Perlin statt Random: benachbarte Frames hängen zusammen, das liest sich
            // als Erschütterung statt als Flimmern. Quadratisch abklingend.
            float decay = 1f - elapsed / duration;
            float falloff = decay * decay;
            float t = elapsed * frequency;
            float x = (Mathf.PerlinNoise(seed.x + t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, seed.y + t) - 0.5f) * 2f;

            float size = OrthoSize();
            target.localPosition = restPosition + new Vector3(x, y, 0f) * amplitude * falloff * size;
        }

        /// <summary>
        /// Der Ausschlag skaliert mit dem sichtbaren Bildausschnitt, damit er auf jeder
        /// Kamera gleich stark wirkt statt in Metern festgenagelt zu sein.
        /// </summary>
        private float OrthoSize()
        {
            var camera = Camera.main;
            if (camera == null) return 5f;
            return camera.orthographic ? camera.orthographicSize * 2f : 10f;
        }

        private void OnDestroy()
        {
            ReleaseTarget();
            if (Instance == this) Instance = null;
        }
    }
}
