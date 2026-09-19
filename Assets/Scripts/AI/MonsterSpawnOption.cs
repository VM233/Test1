using System;
using UnityEngine;

namespace Test1.Combat.AI
{
    [Serializable]
    public sealed class MonsterSpawnOption
    {
        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        [Min(0.01f)]
        private float weight = 1f;

        public GameObject Prefab => prefab;

        public float Weight => weight;

        public MonsterSpawnOption(
            GameObject monsterPrefab,
            float selectionWeight = 1f)
        {
            prefab = monsterPrefab;
            weight = Mathf.Max(0.01f, selectionWeight);
        }
    }
}
