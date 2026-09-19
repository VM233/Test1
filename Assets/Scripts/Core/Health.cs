using System;
using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField]
        [Min(1f)]
        private float maxHealth = 100f;

        public float MaxHealth => maxHealth;

        public float CurrentHealth { get; private set; }

        public float Normalized => CurrentHealth / maxHealth;

        public bool IsAlive { get; private set; }

        public event Action<Health> Changed;

        public event Action<Health, float, float> ValueChanged;

        public event Action<Health, Combatant> Damaged;

        public event Action<Health> Died;

        public event Action<Health> Revived;

        private void Awake()
        {
            RestoreToFull();
        }

        private void Start()
        {
            Changed?.Invoke(this);
        }

        public void Configure(float value)
        {
            maxHealth = Mathf.Max(1f, value);
        }

        public bool TakeDamage(
            float amount,
            Combatant source = null)
        {
            if (IsAlive == false || amount <= 0f)
            {
                return false;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Damaged?.Invoke(this, source);
            ValueChanged?.Invoke(
                this,
                previousHealth,
                CurrentHealth);
            Changed?.Invoke(this);

            if (CurrentHealth > 0f)
            {
                return true;
            }

            IsAlive = false;
            Died?.Invoke(this);
            return true;
        }

        public void RestoreToFull()
        {
            CurrentHealth = maxHealth;
            IsAlive = true;
        }

        public void Revive()
        {
            float previousHealth = CurrentHealth;
            RestoreToFull();
            Revived?.Invoke(this);
            if (Mathf.Approximately(previousHealth, CurrentHealth) == false)
            {
                ValueChanged?.Invoke(
                    this,
                    previousHealth,
                    CurrentHealth);
            }

            Changed?.Invoke(this);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
