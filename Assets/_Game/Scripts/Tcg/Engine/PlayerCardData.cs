using UnityEngine;

namespace Rouge.Tcg
{
    [CreateAssetMenu(fileName = "NeueSpielerkarte", menuName = "Rouge TCG/Spielerkarte")]
    public class PlayerCardData : CardDefinition
    {
        [Header("Spieler")]
        [Tooltip("Start-Lebenspunkte, die dieser Held mitbringt")]
        public int startLifePoints = 8000;

        public override CardKind Kind => CardKind.Player;

        public override Color FrameColor => new Color(0.85f, 0.72f, 0.25f);
    }
}
