using System;
using System.Collections.Generic;
using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.AI
{
    [DisallowMultipleComponent]
    public sealed class MonsterSpawnArea : MonoBehaviour
    {
        private const float CONTAINMENT_TOLERANCE = 0.0001f;
        private const float OVERLAP_CENTER_HEIGHT = 0.6f;
        private const float OVERLAP_RADIUS = 0.5f;
        private const int OVERLAP_BUFFER_SIZE = 16;

        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField]
        private SpawnAreaShape areaShape = SpawnAreaShape.Circle;

        [SerializeField]
        [Min(0.1f)]
        private float radius = 4f;

        [SerializeField]
        private Vector2 boxSize = new(8f, 8f);

        [SerializeField]
        [Min(0)]
        private int spawnCount = 3;

        [SerializeField]
        [Min(0f)]
        private float minimumSpacing = 1.25f;

        [SerializeField]
        [Min(1)]
        private int placementAttemptsPerMonster = 40;

        [SerializeField]
        private int randomSeed = 20260919;

        [SerializeField]
        private Transform spawnContainer;

        [SerializeField]
        private List<MonsterSpawnOption> spawnOptions = new();

        private readonly List<GameObject> spawnedMonsters = new();
        private readonly List<Vector3> reservedPositions = new();
        private readonly Collider[] overlapBuffer =
            new Collider[OVERLAP_BUFFER_SIZE];

        private bool hasSpawned;

        public IReadOnlyList<GameObject> SpawnedMonsters =>
            spawnedMonsters;

        public int ConfiguredSpawnCount => spawnCount;

        public int ConfiguredOptionCount => spawnOptions.Count;

        public SpawnAreaShape AreaShape => areaShape;

        public float Radius => radius;

        public Vector2 BoxSize => boxSize;

        public event Action<GameObject> MonsterSpawned;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAll();
            }
        }

        public void Configure(
            GameObject monsterPrefab,
            int count,
            SpawnAreaShape shape,
            float circleRadius,
            Vector2 rectangularSize,
            float spacing,
            int seed)
        {
            spawnOptions.Clear();
            spawnOptions.Add(new MonsterSpawnOption(monsterPrefab));
            spawnCount = Mathf.Max(0, count);
            areaShape = shape;
            radius = Mathf.Max(0.1f, circleRadius);
            boxSize = new Vector2(
                Mathf.Max(0.1f, rectangularSize.x),
                Mathf.Max(0.1f, rectangularSize.y));
            minimumSpacing = Mathf.Max(0f, spacing);
            randomSeed = seed;
        }

        public void SpawnAll()
        {
            if (hasSpawned)
            {
                return;
            }

            hasSpawned = true;
            RemoveDestroyedEntries();
            EnsureSpawnContainer();

            var random = new System.Random(randomSeed);
            for (int index = 0; index < spawnCount; index++)
            {
                GameObject prefab = SelectPrefab(random);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(MonsterSpawnArea)} on '{name}' has no " +
                        "valid monster prefab.");
                }

                if (TryFindSpawnPosition(random, out Vector3 position) ==
                    false)
                {
                    Debug.LogWarning(
                        $"{name} could only place " +
                        $"{spawnedMonsters.Count} of {spawnCount} " +
                        "monsters. Increase the area or reduce minimum " +
                        "spacing.",
                        this);
                    break;
                }

                float yaw = (float)(random.NextDouble() * 360d);
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                GameObject monster = Instantiate(
                    prefab,
                    position,
                    rotation,
                    spawnContainer);
                monster.name = $"{prefab.name} {index + 1}";

                RespawnController respawn =
                    monster.GetComponent<RespawnController>() ??
                    throw new MissingComponentException(
                        $"Spawned monster '{monster.name}' requires " +
                        $"{nameof(RespawnController)}.");
                respawn.SetSpawnPose(position, rotation);

                spawnedMonsters.Add(monster);
                reservedPositions.Add(position);
                MonsterSpawned?.Invoke(monster);
            }
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            if (areaShape == SpawnAreaShape.Circle)
            {
                return new Vector2(localPoint.x, localPoint.z).sqrMagnitude <=
                       radius * radius + CONTAINMENT_TOLERANCE;
            }

            Vector2 halfSize = boxSize * 0.5f;
            return Mathf.Abs(localPoint.x) <=
                   halfSize.x + CONTAINMENT_TOLERANCE &&
                   Mathf.Abs(localPoint.z) <=
                   halfSize.y + CONTAINMENT_TOLERANCE;
        }

        private GameObject SelectPrefab(System.Random random)
        {
            float totalWeight = 0f;
            GameObject fallback = null;
            foreach (MonsterSpawnOption option in spawnOptions)
            {
                if (option == null || option.Prefab == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0.01f, option.Weight);
                fallback = option.Prefab;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float selection = (float)random.NextDouble() * totalWeight;
            foreach (MonsterSpawnOption option in spawnOptions)
            {
                if (option == null || option.Prefab == null)
                {
                    continue;
                }

                selection -= Mathf.Max(0.01f, option.Weight);
                if (selection <= 0f)
                {
                    return option.Prefab;
                }
            }

            return fallback;
        }

        private bool TryFindSpawnPosition(
            System.Random random,
            out Vector3 position)
        {
            position = transform.position;
            for (int attempt = 0;
                 attempt < placementAttemptsPerMonster;
                 attempt++)
            {
                Vector3 localPoint = GenerateLocalPoint(random);
                Vector3 candidate = transform.TransformPoint(localPoint);
                if (HasRequiredSpacing(candidate) == false ||
                    OverlapsCombatant(candidate))
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            return false;
        }

        private Vector3 GenerateLocalPoint(System.Random random)
        {
            if (areaShape == SpawnAreaShape.Circle)
            {
                float angle =
                    (float)(random.NextDouble() * Math.PI * 2d);
                float distance =
                    Mathf.Sqrt((float)random.NextDouble()) * radius;
                return new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance);
            }

            float x = ((float)random.NextDouble() - 0.5f) * boxSize.x;
            float z = ((float)random.NextDouble() - 0.5f) * boxSize.y;
            return new Vector3(x, 0f, z);
        }

        private bool HasRequiredSpacing(Vector3 candidate)
        {
            float requiredDistanceSqr = minimumSpacing * minimumSpacing;
            foreach (Vector3 reservedPosition in reservedPositions)
            {
                Vector3 offset = candidate - reservedPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude < requiredDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private bool OverlapsCombatant(Vector3 candidate)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                candidate + Vector3.up * OVERLAP_CENTER_HEIGHT,
                OVERLAP_RADIUS,
                overlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                Collider hit = overlapBuffer[index];
                if (hit != null &&
                    hit.GetComponentInParent<Combatant>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureSpawnContainer()
        {
            if (spawnContainer != null)
            {
                return;
            }

            var container = new GameObject("Runtime Monsters");
            spawnContainer = container.transform;
            spawnContainer.SetParent(transform, false);
        }

        private void RemoveDestroyedEntries()
        {
            for (int index = spawnedMonsters.Count - 1;
                 index >= 0;
                 index--)
            {
                if (spawnedMonsters[index] == null)
                {
                    spawnedMonsters.RemoveAt(index);
                }
            }
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            boxSize.x = Mathf.Max(0.1f, boxSize.x);
            boxSize.y = Mathf.Max(0.1f, boxSize.y);
            spawnCount = Mathf.Max(0, spawnCount);
            minimumSpacing = Mathf.Max(0f, minimumSpacing);
            placementAttemptsPerMonster = Mathf.Max(
                1,
                placementAttemptsPerMonster);
        }
    }
}
