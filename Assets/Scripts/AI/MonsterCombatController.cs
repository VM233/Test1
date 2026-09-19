using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorMotor))]
    [RequireComponent(typeof(TargetSensor))]
    [RequireComponent(typeof(MeleeAttack))]
    [RequireComponent(typeof(Health))]
    public sealed class MonsterCombatController : MonoBehaviour
    {
        [SerializeField]
        private ActorMotor motor;

        [SerializeField]
        private TargetSensor sensor;

        [SerializeField]
        private MeleeAttack attack;

        [SerializeField]
        private Health health;

        public MonsterActionState State { get; private set; }

        private void Awake()
        {
            motor = GetComponent<ActorMotor>();
            sensor = GetComponent<TargetSensor>();
            attack = GetComponent<MeleeAttack>();
            health = GetComponent<Health>();
        }

        public void Configure(
            ActorMotor actorMotor,
            TargetSensor targetSensor,
            MeleeAttack meleeAttack,
            Health actorHealth)
        {
            motor = actorMotor;
            sensor = targetSensor;
            attack = meleeAttack;
            health = actorHealth;
        }

        private void Update()
        {
            if (health.IsAlive == false)
            {
                motor.SetMovementEnabled(false);
                State = MonsterActionState.Dead;
                return;
            }

            Combatant target = attack.IsAttacking
                ? attack.CurrentTarget
                : sensor.FindClosest();
            if (attack.IsAttacking)
            {
                motor.SetMovementEnabled(false);
                if (target != null && target.IsAlive)
                {
                    motor.FaceTowards(target.Position);
                }

                State = MonsterActionState.Attacking;
                return;
            }

            motor.SetMovementEnabled(true);
            if (target == null || target.IsAlive == false)
            {
                motor.Stop();
                State = MonsterActionState.Idle;
                return;
            }

            Vector3 direction = target.Position - transform.position;
            direction.y = 0f;
            bool targetInRange = attack.IsTargetInRange(target);
            if (targetInRange)
            {
                motor.Stop();
            }
            else
            {
                motor.SetDesiredDirection(direction.normalized);
            }

            motor.FaceTowards(target.Position);
            if (targetInRange)
            {
                if (attack.TryStart(target))
                {
                    motor.SetMovementEnabled(false);
                }
            }

            State = attack.IsAttacking
                ? MonsterActionState.Attacking
                : targetInRange
                    ? MonsterActionState.Idle
                    : MonsterActionState.Chasing;
        }
    }
}
