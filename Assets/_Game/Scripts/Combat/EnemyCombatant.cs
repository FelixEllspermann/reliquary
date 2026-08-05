using UnityEngine;
using Rouge.Data;

namespace Rouge.Combat
{
    public class EnemyCombatant : Combatant
    {
        [Header("Konfiguration")]
        [SerializeField]
        [Tooltip("Gegner-Werte (ScriptableObject) — hier balancen!")]
        private EnemyData data;

        private int intentIndex;

        public EnemyData Data => data;

        public EnemyIntent CurrentIntent =>
            data != null && data.pattern.Count > 0 ? data.pattern[intentIndex % data.pattern.Count] : null;

        public void Initialize()
        {
            if (data == null)
            {
                Debug.LogError("EnemyCombatant: Keine EnemyData zugewiesen!", this);
                return;
            }
            SetupStats(data.enemyName, data.maxHealth);
            intentIndex = 0;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (data.sprite != null) spriteRenderer.sprite = data.sprite;
                spriteRenderer.color = data.tint;
            }
        }

        public void ExecuteIntent(Combatant target)
        {
            var intent = CurrentIntent;
            if (intent == null) return;

            switch (intent.type)
            {
                case IntentType.Attack: target.TakeDamage(intent.value); break;
                case IntentType.Defend: GainBlock(intent.value); break;
            }

            intentIndex++;
            Notify();
        }
    }
}
