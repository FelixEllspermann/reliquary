using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rouge.Data;

namespace Rouge.Combat
{
    public enum CombatState { PlayerTurn, EnemyTurn, Victory, Defeat }

    public class CombatManager : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private PlayerCombatant player;
        [SerializeField] private EnemyCombatant enemy;
        [SerializeField] private DeckManager deck;

        [Header("Ablauf")]
        [Range(0f, 3f)]
        [Tooltip("Pause vor und nach der Gegner-Aktion in Sekunden")]
        [SerializeField] private float enemyTurnDelay = 0.6f;

        public CombatState State { get; private set; }
        public int Energy { get; private set; }
        public int MaxEnergy => player != null && player.Config != null ? player.Config.energyPerTurn : 3;
        public int TurnNumber { get; private set; }
        public PlayerCombatant Player => player;
        public EnemyCombatant Enemy => enemy;
        public DeckManager Deck => deck;

        public event Action OnCombatChanged;

        private void Start()
        {
            StartCombat();
        }

        public void StartCombat()
        {
            if (player == null || enemy == null || deck == null)
            {
                Debug.LogError("CombatManager: Referenzen fehlen (Player/Enemy/Deck im Inspector zuweisen)!", this);
                return;
            }

            player.Initialize();
            enemy.Initialize();
            deck.Initialize(player.Config.startingDeck, player.Config.maxHandSize);

            enemy.OnDied += HandleEnemyDied;
            player.OnDied += HandlePlayerDied;

            TurnNumber = 0;
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            if (State == CombatState.Victory || State == CombatState.Defeat) return;

            TurnNumber++;
            State = CombatState.PlayerTurn;
            player.ResetBlock();
            Energy = MaxEnergy;
            deck.Draw(player.Config.drawPerTurn);
            Notify();
        }

        public bool TryPlayCard(CardData card)
        {
            if (State != CombatState.PlayerTurn || card == null) return false;
            if (!deck.Hand.Contains(card) || Energy < card.cost) return false;

            Energy -= card.cost;
            deck.DiscardFromHand(card);
            ApplyEffects(card);
            Notify();
            return true;
        }

        private void ApplyEffects(CardData card)
        {
            foreach (var effect in card.effects)
            {
                switch (effect.type)
                {
                    case EffectType.Damage:
                        if (card.target == TargetType.Enemy) enemy.TakeDamage(effect.value);
                        else player.TakeDamage(effect.value);
                        break;
                    case EffectType.Block:
                        player.GainBlock(effect.value);
                        break;
                    case EffectType.Heal:
                        player.Heal(effect.value);
                        break;
                    case EffectType.DrawCards:
                        deck.Draw(effect.value);
                        break;
                    case EffectType.GainEnergy:
                        Energy += effect.value;
                        break;
                }
            }
        }

        public void EndPlayerTurn()
        {
            if (State != CombatState.PlayerTurn) return;

            deck.DiscardHand();
            State = CombatState.EnemyTurn;
            Notify();
            StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            yield return new WaitForSeconds(enemyTurnDelay);
            if (State != CombatState.EnemyTurn) yield break;

            enemy.ResetBlock();
            enemy.ExecuteIntent(player);
            Notify();

            yield return new WaitForSeconds(enemyTurnDelay);
            if (State == CombatState.EnemyTurn) StartPlayerTurn();
        }

        public void RestartCombat()
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void HandleEnemyDied()
        {
            State = CombatState.Victory;
            Notify();
        }

        private void HandlePlayerDied()
        {
            State = CombatState.Defeat;
            Notify();
        }

        private void Notify() => OnCombatChanged?.Invoke();
    }
}
