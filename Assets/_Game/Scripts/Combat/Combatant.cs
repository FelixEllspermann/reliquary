using System;
using UnityEngine;

namespace Rouge.Combat
{
    public class Combatant : MonoBehaviour
    {
        [Header("Laufzeit-Werte (werden beim Kampfstart gesetzt)")]
        [SerializeField] protected string displayName = "Kämpfer";
        [SerializeField] protected int maxHealth = 50;
        [SerializeField] protected int currentHealth = 50;
        [SerializeField] protected int block;

        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int Block => block;
        public bool IsDead => currentHealth <= 0;

        public event Action OnStatsChanged;
        public event Action OnDied;

        protected void SetupStats(string name, int maxHp)
        {
            displayName = name;
            maxHealth = maxHp;
            currentHealth = maxHp;
            block = 0;
            Notify();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;

            int absorbed = Mathf.Min(block, amount);
            block -= absorbed;
            currentHealth = Mathf.Max(0, currentHealth - (amount - absorbed));
            Notify();

            if (IsDead) OnDied?.Invoke();
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0 || IsDead) return;
            block += amount;
            Notify();
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Notify();
        }

        public void ResetBlock()
        {
            block = 0;
            Notify();
        }

        protected void Notify() => OnStatsChanged?.Invoke();
    }
}
