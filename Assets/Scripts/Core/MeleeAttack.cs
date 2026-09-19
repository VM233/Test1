using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Combatant))]
    public sealed class MeleeAttack : MonoBehaviour
    {
        [SerializeField]
        [Min(0f)]
        private float damage = 20f;

        [SerializeField]
        [Min(0.1f)]
        private float range = 1.6f;

        [SerializeField]
        [Min(0.05f)]
        private float cooldown = 1f;

        [SerializeField]
        [FormerlySerializedAs("windup")]
        [Min(0f)]
        private float hitDelay = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float recovery = 0.35f;

        [SerializeField]
        [Min(0f)]
        private float hitTolerance = 0.3f;

        private Combatant owner;
        private Coroutine attackRoutine;
        private float nextAttackTime;

        public bool IsAttacking { get; private set; }

        public float Range => range;

        public Combatant CurrentTarget { get; private set; }

        public event Action<MeleeAttack> AttackStarted;

        public event Action<MeleeAttack, Combatant> HitLanded;

        public event Action<MeleeAttack> AttackFinished;

        private void Awake()
        {
            owner = GetComponent<Combatant>();
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

        public void Configure(
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float attackHitDelay,
            float attackRecovery)
        {
            damage = Mathf.Max(0f, attackDamage);
            range = Mathf.Max(0.1f, attackRange);
            cooldown = Mathf.Max(0.05f, attackCooldown);
            hitDelay = Mathf.Max(0f, attackHitDelay);
            recovery = Mathf.Max(0f, attackRecovery);
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
            float allowedRange = range + Mathf.Max(0f, extraRange);
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
                             Mathf.Max(cooldown, hitDelay + recovery);
            attackRoutine = StartCoroutine(AttackSequence(target));
            return true;
        }

        private IEnumerator AttackSequence(Combatant target)
        {
            IsAttacking = true;
            CurrentTarget = target;
            AttackStarted?.Invoke(this);

            if (hitDelay > 0f)
            {
                yield return new WaitForSeconds(hitDelay);
            }

            if (owner.IsAlive &&
                IsTargetInRange(target, hitTolerance))
            {
                target.Health.TakeDamage(damage, owner);
                HitLanded?.Invoke(this, target);
            }

            if (recovery > 0f)
            {
                yield return new WaitForSeconds(recovery);
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

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            range = Mathf.Max(0.1f, range);
            cooldown = Mathf.Max(0.05f, cooldown);
            hitDelay = Mathf.Max(0f, hitDelay);
            recovery = Mathf.Max(0f, recovery);
            hitTolerance = Mathf.Max(0f, hitTolerance);
        }
    }
}
