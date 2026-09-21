using UnityEngine;

namespace Test1.Combat.Core
{
    [CreateAssetMenu(
        fileName = "MeleeAttackSettings",
        menuName = "Test1/Combat/Melee Attack Settings")]
    public sealed class MeleeAttackSettings : ScriptableObject
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
        [Min(0f)]
        private float hitDelay = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float recovery = 0.35f;

        [SerializeField]
        [Min(0f)]
        private float hitTolerance = 0.3f;

        public float Damage => damage;

        public float Range => range;

        public float Cooldown => cooldown;

        public float HitDelay => hitDelay;

        public float Recovery => recovery;

        public float HitTolerance => hitTolerance;

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
