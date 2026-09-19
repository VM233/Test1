using System;
using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorMotor))]
    [RequireComponent(typeof(MeleeAttack))]
    [RequireComponent(typeof(Health))]
    public sealed class ActorAnimation : MonoBehaviour
    {
        private const string ATTACK_LAYER_NAME = "Attack Overlay";

        private static readonly int SPEED_PARAMETER_ID =
            Animator.StringToHash("Speed");

        private static readonly int ATTACK_PARAMETER_ID =
            Animator.StringToHash("Attack");

        private static readonly int DIE_PARAMETER_ID =
            Animator.StringToHash("Die");

        private static readonly int RESPAWN_PARAMETER_ID =
            Animator.StringToHash("Respawn");

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private ActorMotor motor;

        [SerializeField]
        private MeleeAttack attack;

        [SerializeField]
        private Health health;

        private int attackLayerIndex;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>() ??
                throw new MissingComponentException(
                    $"{nameof(ActorAnimation)} on '{name}' requires an " +
                    $"{nameof(Animator)} in its hierarchy.");
            motor = GetComponent<ActorMotor>();
            attack = GetComponent<MeleeAttack>();
            health = GetComponent<Health>();
            ResolveAttackLayer();
        }

        private void OnEnable()
        {
            attack.AttackStarted += OnAttackStarted;
            health.Died += OnDied;
            health.Revived += OnRevived;
        }

        private void OnDisable()
        {
            attack.AttackStarted -= OnAttackStarted;
            health.Died -= OnDied;
            health.Revived -= OnRevived;
        }

        private void Update()
        {
            animator.SetFloat(
                SPEED_PARAMETER_ID,
                motor.NormalizedSpeed,
                0.1f,
                Time.deltaTime);
        }

        public void Configure(
            Animator targetAnimator,
            ActorMotor targetMotor,
            MeleeAttack meleeAttack,
            Health actorHealth)
        {
            animator = targetAnimator;
            motor = targetMotor;
            attack = meleeAttack;
            health = actorHealth;
            ResolveAttackLayer();
        }

        private void OnAttackStarted(MeleeAttack meleeAttack)
        {
            animator.ResetTrigger(DIE_PARAMETER_ID);
            animator.SetTrigger(ATTACK_PARAMETER_ID);
        }

        private void OnDied(Health actorHealth)
        {
            animator.ResetTrigger(ATTACK_PARAMETER_ID);
            animator.SetLayerWeight(attackLayerIndex, 0f);
            animator.SetFloat(SPEED_PARAMETER_ID, 0f);
            animator.SetTrigger(DIE_PARAMETER_ID);
        }

        private void OnRevived(Health actorHealth)
        {
            animator.ResetTrigger(DIE_PARAMETER_ID);
            animator.SetLayerWeight(attackLayerIndex, 1f);
            animator.SetTrigger(RESPAWN_PARAMETER_ID);
        }

        private void ResolveAttackLayer()
        {
            attackLayerIndex = animator.GetLayerIndex(ATTACK_LAYER_NAME);
            if (attackLayerIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Animator '{animator.name}' requires layer " +
                    $"'{ATTACK_LAYER_NAME}'.");
            }
        }
    }
}
