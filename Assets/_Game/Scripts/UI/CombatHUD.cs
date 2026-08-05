using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rouge.Combat;
using Rouge.Data;

namespace Rouge.UI
{
    public class CombatHUD : MonoBehaviour
    {
        [Header("Referenzen (im Inspector verdrahten)")]
        [SerializeField] private CombatManager combat;

        [Header("Spieler-Anzeige")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerHealthText;
        [SerializeField] private TMP_Text playerBlockText;

        [Header("Gegner-Anzeige")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHealthText;
        [SerializeField] private TMP_Text enemyBlockText;
        [SerializeField] private TMP_Text enemyIntentText;

        [Header("Zug-Anzeige")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text drawPileText;
        [SerializeField] private TMP_Text discardPileText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Button endTurnButton;

        [Header("Ergebnis")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;

        private void OnEnable()
        {
            if (combat == null) return;
            combat.OnCombatChanged += Refresh;
            if (combat.Deck != null) combat.Deck.OnPilesChanged += Refresh;
            if (combat.Player != null) combat.Player.OnStatsChanged += Refresh;
            if (combat.Enemy != null) combat.Enemy.OnStatsChanged += Refresh;
        }

        private void OnDisable()
        {
            if (combat == null) return;
            combat.OnCombatChanged -= Refresh;
            if (combat.Deck != null) combat.Deck.OnPilesChanged -= Refresh;
            if (combat.Player != null) combat.Player.OnStatsChanged -= Refresh;
            if (combat.Enemy != null) combat.Enemy.OnStatsChanged -= Refresh;
        }

        private void Refresh()
        {
            var player = combat.Player;
            var enemy = combat.Enemy;

            if (player != null)
            {
                if (playerNameText != null) playerNameText.text = player.DisplayName;
                if (playerHealthText != null) playerHealthText.text = $"{player.CurrentHealth} / {player.MaxHealth} LP";
                if (playerBlockText != null) playerBlockText.text = player.Block > 0 ? $"Block: {player.Block}" : "";
            }

            if (enemy != null)
            {
                if (enemyNameText != null) enemyNameText.text = enemy.DisplayName;
                if (enemyHealthText != null) enemyHealthText.text = $"{enemy.CurrentHealth} / {enemy.MaxHealth} LP";
                if (enemyBlockText != null) enemyBlockText.text = enemy.Block > 0 ? $"Block: {enemy.Block}" : "";
                if (enemyIntentText != null) enemyIntentText.text = FormatIntent(enemy);
            }

            if (energyText != null) energyText.text = $"Energie: {combat.Energy} / {combat.MaxEnergy}";
            if (drawPileText != null && combat.Deck != null) drawPileText.text = $"Nachziehstapel: {combat.Deck.DrawPileCount}";
            if (discardPileText != null && combat.Deck != null) discardPileText.text = $"Ablagestapel: {combat.Deck.DiscardPileCount}";
            if (turnText != null) turnText.text = $"Zug {combat.TurnNumber}";
            if (endTurnButton != null) endTurnButton.interactable = combat.State == CombatState.PlayerTurn;

            bool finished = combat.State == CombatState.Victory || combat.State == CombatState.Defeat;
            if (resultPanel != null && resultPanel.activeSelf != finished) resultPanel.SetActive(finished);
            if (resultText != null && finished)
                resultText.text = combat.State == CombatState.Victory ? "Sieg!" : "Niederlage ...";
        }

        private string FormatIntent(EnemyCombatant enemy)
        {
            var intent = enemy.CurrentIntent;
            if (intent == null || enemy.IsDead) return "";

            switch (intent.type)
            {
                case IntentType.Attack: return $"Absicht: {intent.value} Schaden";
                case IntentType.Defend: return $"Absicht: blockt {intent.value}";
                default: return "";
            }
        }
    }
}
