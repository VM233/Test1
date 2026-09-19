using System.Collections;
using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(ActorMotor))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        private float respawnDelay = 3f;

        [SerializeField]
        [Min(0f)]
        private float corpseVisibleDuration = 1.1f;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private ActorMotor motor;

        [SerializeField]
        private Rigidbody body;

        private Health health;
        private Collider[] colliders;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Coroutine respawnRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            motor = GetComponent<ActorMotor>();
            body = GetComponent<Rigidbody>();
            if (visualRoot == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(RespawnController)} on '{name}' requires a " +
                    "visual root.");
            }

            colliders = GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                throw new MissingComponentException(
                    $"{nameof(RespawnController)} on '{name}' requires at " +
                    "least one collider.");
            }

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }

        public void Configure(
            Transform actorVisualRoot,
            ActorMotor actorMotor,
            Rigidbody actorBody,
            float delay,
            float visibleDuration)
        {
            visualRoot = actorVisualRoot;
            motor = actorMotor;
            body = actorBody;
            respawnDelay = Mathf.Max(0.1f, delay);
            corpseVisibleDuration = Mathf.Clamp(
                visibleDuration,
                0f,
                respawnDelay);
        }

        public void SetSpawnPose(
            Vector3 position,
            Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        private void OnDied(Health actorHealth)
        {
            if (respawnRoutine == null)
            {
                respawnRoutine = StartCoroutine(RespawnSequence());
            }
        }

        private IEnumerator RespawnSequence()
        {
            motor.Stop();
            motor.enabled = false;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;

            foreach (Collider actorCollider in colliders)
            {
                actorCollider.enabled = false;
            }

            if (corpseVisibleDuration > 0f)
            {
                yield return new WaitForSeconds(corpseVisibleDuration);
            }

            visualRoot.gameObject.SetActive(false);

            float hiddenDuration = Mathf.Max(
                0f,
                respawnDelay - corpseVisibleDuration);
            if (hiddenDuration > 0f)
            {
                yield return new WaitForSeconds(hiddenDuration);
            }

            transform.SetPositionAndRotation(
                spawnPosition,
                spawnRotation);
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            visualRoot.gameObject.SetActive(true);

            foreach (Collider actorCollider in colliders)
            {
                actorCollider.enabled = true;
            }

            body.isKinematic = false;
            body.WakeUp();
            motor.enabled = true;

            health.Revive();
            respawnRoutine = null;
        }
    }
}
