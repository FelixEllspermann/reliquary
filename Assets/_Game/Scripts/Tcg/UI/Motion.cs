using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Das gemeinsame Bewegungsvokabular aller Animationen (Handoff „Animations",
    /// Abschnitt 0). Drei Kurven, mehr kommt im ganzen Spiel nicht vor — das ist
    /// der Grund, warum sich die Sequenzen wie ein Stück anfühlen.
    ///
    ///   enter(t) = 1 − (1 − t)³             Starts und Auftritte
    ///   drift(t) = 0.5 − 0.5·cos(π·t)       Kamera und Bögen
    ///   pop(t)   = Überschwingen 1.9        Aufschlag
    ///
    /// <see cref="Seg"/> schneidet ein Teilfenster aus dem Szenenfortschritt
    /// heraus und bildet es wieder auf 0…1 ab. Alle Bewegungen laufen über den
    /// Szenenfortschritt, nie über eine eigene Uhr — nur so bleibt eine Szene
    /// dehnbar, ohne dass Bewegung abgeschnitten wird.
    /// </summary>
    public static class Motion
    {
        /// <summary>Ease-out kubisch.</summary>
        public static float Enter(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>Ease-in-out Sinus.</summary>
        public static float Drift(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));

        /// <summary>Ease-out back mit Überschwingen 1.9 — schnappt, statt zu blenden.</summary>
        public static float Pop(float t)
        {
            t = Mathf.Clamp01(t);
            const float s = 1.9f;
            float d = t - 1f;
            return 1f + (s + 1f) * d * d * d + s * d * d;
        }

        /// <summary>Bildet das Fenster [a,b] des Fortschritts p wieder auf 0…1 ab.</summary>
        public static float Seg(float p, float a, float b) =>
            b - a <= 0.0001f ? (p >= b ? 1f : 0f) : Mathf.Clamp01((p - a) / (b - a));

        /// <summary>Lineare Mischung — nur der Kürze halber.</summary>
        public static float Mix(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Farbmischung im selben Sinn.</summary>
        public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        /// <summary>Dieselbe Farbe mit anderer Deckkraft.</summary>
        public static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        /// <summary>Halbwelle eines Sinus über [a,b] — 0 an den Rändern, 1 in der Mitte.</summary>
        public static float Arc(float p, float a, float b) => Mathf.Sin(Mathf.PI * Seg(p, a, b));
    }
}
