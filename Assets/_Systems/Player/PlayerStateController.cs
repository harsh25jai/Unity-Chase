using System;
using UnityEngine;
using Data.Player.Runtime;

namespace Systems.Player
{
    public class PlayerStateController : MonoBehaviour
    {
        [Header("Backend Data")]
        [Tooltip("Direct reference to the Runtime Data container")]
        public PlayerRuntimeData playerRuntimeData;

        [Header("Settings")]
        public float healthRegenRate = 0f;
        public float healthRegenDelay = 3f;

        // Events
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<SurvivalState> OnSurvivalStateChanged;
        public event Action OnPlayerDeathRequested;
        public event Action OnPlayerBustedRequested;

        // Internal
        private bool _isControlLocked;
        private float _lastDamageTime;
        private bool _isIncapacitated;

        private void Start()
        {
            if (playerRuntimeData == null)
            {
                playerRuntimeData = GetComponent<PlayerRuntimeData>();
                if (playerRuntimeData == null)
                {
                    Debug.LogError("[PlayerStateController] No PlayerRuntimeData found!");
                    enabled = false;
                    return;
                }
            }

            // Align internal state
            EvaluateSurvivalState();
        }

        private void Update()
        {
            if (playerRuntimeData.currentSurvivalState == SurvivalState.Alive)
            {
                HandleHealthRegen();
            }
        }

        /// <summary>
        /// Applies damage to the player and updates state.
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (playerRuntimeData.currentSurvivalState == SurvivalState.Dead || 
                playerRuntimeData.currentSurvivalState == SurvivalState.Busted) return;

            // Reduce health
            playerRuntimeData.currentHealth = Mathf.Max(0, playerRuntimeData.currentHealth - amount);
            _lastDamageTime = Time.time;

            Debug.Log($"[PlayerState] Took {amount} damage. Current Health: {playerRuntimeData.currentHealth}");

            // Notify
            OnHealthChanged?.Invoke(playerRuntimeData.currentHealth, playerRuntimeData.maxHealth);

            // Check State
            EvaluateSurvivalState();
        }

        /// <summary>
        /// Restores health up to max.
        /// </summary>
        public void Heal(float amount)
        {
            if (playerRuntimeData.currentSurvivalState == SurvivalState.Dead) return;

            playerRuntimeData.currentHealth = Mathf.Min(playerRuntimeData.maxHealth, playerRuntimeData.currentHealth + amount);
            
            // If we were incapacitated but healed, we might revive (depending on design)
            if (playerRuntimeData.currentSurvivalState == SurvivalState.Incapacitated && playerRuntimeData.currentHealth > 0)
            {
                SetSurvivalState(SurvivalState.Alive);
            }

            OnHealthChanged?.Invoke(playerRuntimeData.currentHealth, playerRuntimeData.maxHealth);
        }

        /// <summary>
        /// Arrest logic.
        /// </summary>
        public void ArrestPlayer()
        {
            if (playerRuntimeData.currentSurvivalState != SurvivalState.Dead)
            {
                SetSurvivalState(SurvivalState.Busted);
                OnPlayerBustedRequested?.Invoke();
            }
        }

        public void SetControlLock(bool locked)
        {
            _isControlLocked = locked;
            // Disable/Enable input processing here or notify InputManager
        }

        private void HandleHealthRegen()
        {
            if (healthRegenRate > 0 && Time.time > _lastDamageTime + healthRegenDelay)
            {
                if (playerRuntimeData.currentHealth < playerRuntimeData.maxHealth)
                {
                    Heal(healthRegenRate * Time.deltaTime);
                }
            }
        }

        private void EvaluateSurvivalState()
        {
            if (playerRuntimeData.currentHealth <= 0)
            {
                if (playerRuntimeData.currentSurvivalState != SurvivalState.Dead && 
                    playerRuntimeData.currentSurvivalState != SurvivalState.Incapacitated)
                {
                    // For now, straight to Dead. If Incapacitated mechanics exist (crawling), switch to that first.
                    SetSurvivalState(SurvivalState.Dead);
                    OnPlayerDeathRequested?.Invoke();
                }
            }
        }

        private void SetSurvivalState(SurvivalState newState)
        {
            if (playerRuntimeData.currentSurvivalState != newState)
            {
                playerRuntimeData.currentSurvivalState = newState;
                OnSurvivalStateChanged?.Invoke(newState);
                Debug.Log($"[PlayerState] State changed to: {newState}");
            }
        }
    }
}
