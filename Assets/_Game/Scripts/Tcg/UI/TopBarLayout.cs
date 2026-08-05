using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Ordnet die Elemente der oberen Leiste von rechts nach links mit festen
    /// Abständen an. Ausgeblendete Elemente (z.B. der Logout-Knopf im Offline-Modus)
    /// werden übersprungen, damit keine Lücke entsteht und nichts überlappt.
    /// </summary>
    [ExecuteAlways]
    public class TopBarLayout : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            public RectTransform target;

            [Tooltip("Abstand zum Element rechts daneben")]
            public float gapBefore = 14f;
        }

        [Tooltip("Von rechts nach links. Das erste Element sitzt am rechten Rand.")]
        [SerializeField] private Slot[] slots = new Slot[0];

        [Tooltip("Abstand des ersten Elements zum rechten Rand der Leiste")]
        [SerializeField] private float rightMargin = 64f;

        private int lastSignature;

        private void OnEnable() { lastSignature = 0; Apply(); }

        private void LateUpdate()
        {
            // Nur neu setzen, wenn sich Sichtbarkeit oder Breiten geändert haben
            int signature = Signature();
            if (signature == lastSignature) return;
            lastSignature = signature;
            Apply();
        }

        private int Signature()
        {
            int hash = 17;
            foreach (var slot in slots)
            {
                if (slot == null || slot.target == null) { hash = hash * 31; continue; }
                hash = hash * 31 + (slot.target.gameObject.activeSelf ? 1 : 0);
                hash = hash * 31 + Mathf.RoundToInt(slot.target.rect.width);
            }
            return hash;
        }

        [ContextMenu("Neu anordnen")]
        public void Apply()
        {
            float cursor = rightMargin;
            bool first = true;
            foreach (var slot in slots)
            {
                if (slot == null || slot.target == null) continue;
                var rect = slot.target;
                if (!rect.gameObject.activeSelf) continue;

                if (!first) cursor += slot.gapBefore;
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-cursor, 0f);
                cursor += rect.rect.width;
                first = false;
            }
        }
    }
}
