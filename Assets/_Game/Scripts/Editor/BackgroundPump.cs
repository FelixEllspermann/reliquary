using UnityEditor;
using UnityEngine;

namespace Rouge.Tcg.EditorTools
{
    /// <summary>
    /// Kontrollierter Hintergrund-Pump für automatisierte Play-Mode-Tests:
    /// tickt die Player-Loop auch ohne Editor-Fokus — aber gedrosselt (30 Hz),
    /// nur solange explizit aktiviert, und schaltet sich beim Verlassen des
    /// Play Mode IMMER selbst ab. Ersetzt die frühere anonyme update-Lambda,
    /// die nach Tests weiterlief und den Editor im Hintergrund dauerhaft
    /// rendern ließ (Symptom: grauer, eingefrorener Editor nach Alt-Tab).
    /// </summary>
    [InitializeOnLoad]
    public static class BackgroundPump
    {
        private const string FlagKey = "rouge_background_pump";
        private const double TargetInterval = 1.0 / 30.0; // 30 Hz reicht für Tests

        private static double lastTick;

        static BackgroundPump()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static bool Active
        {
            get => SessionState.GetBool(FlagKey, false);
            set
            {
                SessionState.SetBool(FlagKey, value);
                if (EditorApplication.isPlaying) Application.runInBackground = value;
            }
        }

        [MenuItem("Rouge/Background Pump/Aktivieren (nur für Tests)")]
        public static void Enable() => Active = true;

        [MenuItem("Rouge/Background Pump/Deaktivieren")]
        public static void Disable() => Active = false;

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            // Nach jedem Play-Mode-Ende ist der Pump garantiert aus.
            if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
                Active = false;
        }

        private static void Tick()
        {
            if (!Active || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            double now = EditorApplication.timeSinceStartup;
            if (now - lastTick < TargetInterval) return;
            lastTick = now;
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
