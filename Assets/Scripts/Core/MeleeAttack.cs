using System;
using System.Collections;
using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Combatant))]
    public sealed class MeleeAttack : MonoBehaviour
    {
        [SerializeField]
        private MeleeAttackSettings settings;

        private Combatant owner;
        private Coroutine attackRoutine;
        private float nextAttackTime;

        public bool IsAttacking { get; private set; }

        public float Range => settings.Range;

        public Combatant CurrentTarget { get; private set; }

        public event Action<MeleeAttack> AttackStarted;

        public event Action<MeleeAttack, Combatant> HitLanded;

        public event Action<MeleeAttack> AttackFinished;

        private void Awake()
        {
            owner = GetComponent<Combatant>();
            if (settings == null)
            {
                throw new MissingReferenceException(
                    $"{name} requires melee attack settings.");
            }
        }

        private void OnEnable()
        {
            owner.Health.Died += OnOwnerDied;
        }

        private void OnDisable()
        {
            owner.Health.Died -= OnOwnerDied;
            CancelAttack();
        }

        public void Configure(MeleeAttackSettings value)
        {
            settings = value;
        }

        public bool IsTargetInRange(
            Combatant target,
            float extraRange = 0f)
        {
            if (target == null || target.IsAlive == false)
            {
                return false;
            }

            Vector3 offset = target.Position - transform.position;
            offset.y = 0f;
            float allowedRange = settings.Range + Mathf.Max(0f, extraRange);
            return offset.sqrMagnitude <= allowedRange * allowedRange;
        }

        public bool TryStart(Combatant target)
        {
            if (IsAttacking ||
                Time.time < nextAttackTime ||
                owner.IsAlive == false)
            {
                return false;
            }

            if (target == null ||
                target.Team == owner.Team ||
                IsTargetInRange(target) == false)
            {
                return false;
            }

            nextAttackTime = Time.time +
                             Mathf.Max(
                                 settings.Cooldown,
                                 settings.HitDelay + settings.Recovery);
            attackRoutine = StartCoroutine(AttackSequence(target));
            return true;
        }

        private IEnumerator AttackSequence(Combatant target)
        {
            IsAttacking = true;
            CurrentTarget = target;
            AttackStarted?.Invoke(this);

            if (settings.HitDelay > 0f)
            {
                yield return new WaitForSeconds(settings.HitDelay);
            }

            if (owner.IsAlive &&
                IsTargetInRange(target, settings.HitTolerance))
            {
                target.Health.TakeDamage(settings.Damage, owner);
                HitLanded?.Invoke(this, target);
            }

            if (settings.Recovery > 0f)
            {
                yield return new WaitForSeconds(settings.Recovery);
            }

            FinishAttack();
        }

        private void OnOwnerDied(Health health)
        {
            CancelAttack();
        }

        private void CancelAttack()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            if (IsAttacking)
            {
                FinishAttack();
            }
        }

        private void FinishAttack()
        {
            attackRoutine = null;
            IsAttacking = false;
            CurrentTarget = null;
            AttackFinished?.Invoke(this);
        }

    }
}
