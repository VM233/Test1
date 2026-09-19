using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.AI
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorMotor))]
    public sealed class SeparationSteering : MonoBehaviour
    {
        private const float MINIMUM_DISTANCE_SQR = 0.0001f;

        private static readonly List<SeparationSteering> activeAgents =
            new();

        [SerializeField]
        [Min(0.1f)]
        private float separationRadius = 1.15f;

        [SerializeField]
        [Min(0f)]
        private float separationStrength = 2.25f;

        private ActorMotor motor;

        private void Awake()
        {
            motor = GetComponent<ActorMotor>();
        }

        private void OnEnable()
        {
            if (activeAgents.Contains(this) == false)
            {
                activeAgents.Add(this);
            }
        }

        private void OnDisable()
        {
            activeAgents.Remove(this);
            motor.SetSteeringVelocity(Vector3.zero);
        }

        public void Configure(float radius, float strength)
        {
            separationRadius = Mathf.Max(0.1f, radius);
            separationStrength = Mathf.Max(0f, strength);
        }

        private void FixedUpdate()
        {
            if (motor.enabled == false)
            {
                return;
            }

            Vector3 push = Vector3.zero;
            int neighborCount = 0;
            float radiusSqr = separationRadius * separationRadius;

            for (int index = activeAgents.Count - 1;
                 index >= 0;
                 index--)
            {
                SeparationSteering other = activeAgents[index];
                if (other == null)
                {
                    activeAgents.RemoveAt(index);
                    continue;
                }

                if (ReferenceEquals(other, this) ||
                    other.isActiveAndEnabled == false)
                {
                    continue;
                }

                Vector3 away = transform.position -
                               other.transform.position;
                away.y = 0f;
                float sqrDistance = away.sqrMagnitude;
                if (sqrDistance >= radiusSqr)
                {
                    continue;
                }

                if (sqrDistance < MINIMUM_DISTANCE_SQR)
                {
                    int thisIdentity = RuntimeHelpers.GetHashCode(this);
                    int otherIdentity = RuntimeHelpers.GetHashCode(other);
                    away = thisIdentity < otherIdentity
                        ? Vector3.left
                        : Vector3.right;
                    sqrDistance = MINIMUM_DISTANCE_SQR;
                }

                float distance = Mathf.Sqrt(sqrDistance);
                float weight = 1f - distance / separationRadius;
                push += away / distance * weight;
                neighborCount++;
            }

            if (neighborCount > 0)
            {
                push /= neighborCount;
                push = Vector3.ClampMagnitude(
                    push * separationStrength,
                    separationStrength);
            }

            motor.SetSteeringVelocity(push);
        }
    }
}
