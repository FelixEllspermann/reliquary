using UnityEngine;
using Rouge.Data;

namespace Rouge.Combat
{
    public class PlayerCombatant : Combatant
    {
        [Header("Konfiguration")]
        [SerializeField]
        [Tooltip("Spieler-Werte (ScriptableObject) — hier balancen!")]
        private PlayerConfig config;

        public PlayerConfig Config => config;

        public void Initialize()
        {
            if (config == null)
            {
                Debug.LogError("PlayerCombatant: Keine PlayerConfig zugewiesen!", this);
                return;
            }
            SetupStats(config.playerName, config.maxHealth);
        }
    }
}
