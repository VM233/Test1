using UnityEngine;

namespace Test1.Combat.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ActorMotor : MonoBehaviour
    {
        private const float DIRECTION_SQR_THRESHOLD = 0.0001f;
        private const float SPEED_EPSILON = 0.001f;

        [SerializeField]
        [Min(0f)]
        private float moveSpeed = 4f;

        [SerializeField]
        [Min(0f)]
        private float turnSpeed = 720f;

        private Rigidbody body;
        private Vector3 desiredDirection;
        private Vector3 facingDirection;
        private Vector3 steeringVelocity;
        private float speedScale;
        private bool movementEnabled = true;

        public float MoveSpeed => moveSpeed;

        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Configure(float speed, float degreesPerSecond)
        {
            moveSpeed = Mathf.Max(0f, speed);
            turnSpeed = Mathf.Max(0f, degreesPerSecond);
        }

        public void SetDesiredDirection(
            Vector3 direction,
            float normalizedSpeed = 1f)
        {
            direction.y = 0f;
            desiredDirection = direction.sqrMagnitude > 1f
                ? direction.normalized
                : direction;
            speedScale = Mathf.Clamp01(normalizedSpeed);

            if (desiredDirection.sqrMagnitude > DIRECTION_SQR_THRESHOLD)
            {
                facingDirection = desiredDirection.normalized;
            }
        }

        public void SetSteeringVelocity(Vector3 velocity)
        {
            velocity.y = 0f;
            steeringVelocity = velocity;
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > DIRECTION_SQR_THRESHOLD)
            {
                facingDirection = direction.normalized;
            }
        }

        public void Stop()
        {
            desiredDirection = Vector3.zero;
            speedScale = 0f;
        }

        public void SetMovementEnabled(bool value)
        {
            movementEnabled = value;
            if (movementEnabled == false)
            {
                Stop();
            }
        }

        private void FixedUpdate()
        {
            if (body.isKinematic)
            {
                NormalizedSpeed = 0f;
                return;
            }

            Vector3 planarVelocity = movementEnabled
                ? desiredDirection * (moveSpeed * speedScale) +
                  steeringVelocity
                : Vector3.zero;
            Vector3 currentVelocity = body.linearVelocity;
            body.linearVelocity = new Vector3(
                planarVelocity.x,
                currentVelocity.y,
                planarVelocity.z);
            NormalizedSpeed = moveSpeed <= SPEED_EPSILON
                ? 0f
                : Mathf.Clamp01(
                    new Vector2(
                        planarVelocity.x,
                        planarVelocity.z).magnitude / moveSpeed);

            if (facingDirection.sqrMagnitude > DIRECTION_SQR_THRESHOLD)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    facingDirection,
                    Vector3.up);
                Quaternion rotation = Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime);
                body.MoveRotation(rotation);
            }

            steeringVelocity = Vector3.zero;
        }

        private void OnDisable()
        {
            desiredDirection = Vector3.zero;
            steeringVelocity = Vector3.zero;
            speedScale = 0f;
            movementEnabled = true;
            NormalizedSpeed = 0f;

            if (body.isKinematic == false)
            {
                Vector3 velocity = body.linearVelocity;
                body.linearVelocity = new Vector3(
                    0f,
                    velocity.y,
                    0f);
            }
        }
    }
}
