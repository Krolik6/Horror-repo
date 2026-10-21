using System;
using UnityEngine;

namespace Horror.Player
{
    /// <summary>
    /// Stamina pool. Sprinting and jumping drain it; it regenerates after a short delay.
    /// Running it fully dry makes the player exhausted: no sprinting or jumping until it
    /// recovers past a threshold.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;

        [Tooltip("Stamina per second regained once regeneration starts.")]
        [SerializeField] private float regenPerSecond = 12f;

        [Tooltip("Seconds after the last drain before stamina starts coming back.")]
        [SerializeField] private float regenDelay = 1.25f;

        [Tooltip("After running dry, stamina must climb back to this value before sprinting is allowed again.")]
        [SerializeField] private float exhaustedRecoverThreshold = 35f;

        public float Max => maxStamina;
        public float Current { get; private set; }
        public float Normalized => maxStamina > 0f ? Current / maxStamina : 0f;
        public bool IsExhausted { get; private set; }

        /// <summary>Set by the movement script, e.g. resting while crouched or prone regenerates faster.</summary>
        public float RegenMultiplier { get; set; } = 1f;

        /// <summary>Fires with the normalized value (0-1) whenever stamina changes.</summary>
        public event Action<float> Changed;
        public event Action ExhaustedStarted;
        public event Action ExhaustedEnded;

        private float lastDrainTime = float.NegativeInfinity;

        private void Awake()
        {
            Current = maxStamina;
        }

        public bool CanUse(float amount) => !IsExhausted && Current >= amount;

        /// <summary>Continuous drain, e.g. sprint cost multiplied by deltaTime.</summary>
        public void Drain(float amount)
        {
            if (amount <= 0f) return;

            lastDrainTime = Time.time;
            SetCurrent(Current - amount);

            if (Current <= 0f && !IsExhausted)
            {
                IsExhausted = true;
                ExhaustedStarted?.Invoke();
            }
        }

        /// <summary>One-off cost, e.g. a jump. Returns false and spends nothing if there isn't enough.</summary>
        public bool TryConsume(float amount)
        {
            if (!CanUse(amount)) return false;
            Drain(amount);
            return true;
        }

        private void Update()
        {
            if (Current >= maxStamina || Time.time - lastDrainTime < regenDelay) return;

            SetCurrent(Current + regenPerSecond * RegenMultiplier * Time.deltaTime);

            if (IsExhausted && Current >= exhaustedRecoverThreshold)
            {
                IsExhausted = false;
                ExhaustedEnded?.Invoke();
            }
        }

        private void SetCurrent(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxStamina);
            if (Mathf.Approximately(clamped, Current)) return;
            Current = clamped;
            Changed?.Invoke(Normalized);
        }
    }
}
