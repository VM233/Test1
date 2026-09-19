using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    public sealed class TargetSensor : MonoBehaviour
    {
        [SerializeField]
        private ActorTeam targetTeam = ActorTeam.Monster;

        [SerializeField]
        [Min(0.1f)]
        private float detectionRadius = 6f;

        public void Configure(ActorTeam value, float radius)
        {
            targetTeam = value;
            detectionRadius = Mathf.Max(0.1f, radius);
        }

        public Combatant FindClosest()
        {
            return Combatant.FindClosest(
                targetTeam,
                transform.position,
                detectionRadius);
        }

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
        }
    }
}
