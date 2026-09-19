using System.Collections.Generic;
using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class Combatant : MonoBehaviour
    {
        private static readonly List<Combatant> activeCombatants = new();

        [SerializeField]
        private ActorTeam team;

        [SerializeField]
        private Transform aimPoint;

        private Health health;

        public ActorTeam Team => team;

        public Health Health => health != null
            ? health
            : health = GetComponent<Health>();

        public bool IsAlive => Health.IsAlive;

        public Vector3 Position => aimPoint != null
            ? aimPoint.position
            : transform.position;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (activeCombatants.Contains(this) == false)
            {
                activeCombatants.Add(this);
            }
        }

        private void OnDisable()
        {
            activeCombatants.Remove(this);
        }

        public void Configure(
            ActorTeam value,
            Transform targetPoint = null)
        {
            team = value;
            aimPoint = targetPoint;
        }

        public static Combatant FindClosest(
            ActorTeam targetTeam,
            Vector3 origin,
            float range)
        {
            Combatant closest = null;
            float closestSqrDistance = range * range;

            for (int index = activeCombatants.Count - 1;
                 index >= 0;
                 index--)
            {
                Combatant candidate = activeCombatants[index];
                if (candidate == null)
                {
                    activeCombatants.RemoveAt(index);
                    continue;
                }

                if (candidate.isActiveAndEnabled == false ||
                    candidate.IsAlive == false ||
                    candidate.team != targetTeam)
                {
                    continue;
                }

                Vector3 offset = candidate.Position - origin;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > closestSqrDistance)
                {
                    continue;
                }

                closest = candidate;
                closestSqrDistance = sqrDistance;
            }

            return closest;
        }
    }
}
