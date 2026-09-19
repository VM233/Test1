using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputSource))]
    [RequireComponent(typeof(ActorMotor))]
    [RequireComponent(typeof(TargetSensor))]
    [RequireComponent(typeof(MeleeAttack))]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        private const float MOVEMENT_SQR_THRESHOLD = 0.001f;

        [SerializeField]
        private PlayerInputSource input;

        [SerializeField]
        private ActorMotor motor;

        [SerializeField]
        private TargetSensor sensor;

        [SerializeField]
        private MeleeAttack attack;

        [SerializeField]
        private Health health;

        public PlayerActionState State { get; private set; }

        private void Awake()
        {
            input = GetComponent<PlayerInputSource>();
            motor = GetComponent<ActorMotor>();
            sensor = GetComponent<TargetSensor>();
            attack = GetComponent<MeleeAttack>();
            health = GetComponent<Health>();
        }

        public void Configure(
            PlayerInputSource inputSource,
            ActorMotor actorMotor,
            TargetSensor targetSensor,
            MeleeAttack meleeAttack,
            Health actorHealth)
        {
            input = inputSource;
            motor = actorMotor;
            sensor = targetSensor;
            attack = meleeAttack;
            health = actorHealth;
        }

        private void Update()
        {
            if (health.IsAlive == false)
            {
                motor.Stop();
                State = PlayerActionState.Dead;
                return;
            }

            Vector2 moveInput = input.Move;
            Vector3 direction = new(
                moveInput.x,
                0f,
                moveInput.y);
            motor.SetDesiredDirection(
                direction,
                moveInput.magnitude);

            if (attack.IsAttacking)
            {
                if (attack.CurrentTarget != null)
                {
                    motor.FaceTowards(attack.CurrentTarget.Position);
                }

                State = PlayerActionState.Attacking;
                return;
            }

            Combatant target = sensor.FindClosest();
            if (target != null && attack.IsTargetInRange(target))
            {
                motor.FaceTowards(target.Position);
                attack.TryStart(target);
                State = attack.IsAttacking
                    ? PlayerActionState.Attacking
                    : GetLocomotionState(direction);
                return;
            }

            State = GetLocomotionState(direction);
        }

        private static PlayerActionState GetLocomotionState(
            Vector3 direction)
        {
            return direction.sqrMagnitude > MOVEMENT_SQR_THRESHOLD
                ? PlayerActionState.Moving
                : PlayerActionState.Idle;
        }
    }
}
