using UnityEngine;

namespace Test1.Combat.Camera
{
    [DisallowMultipleComponent]
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector3 offset = new(0f, 10f, -9f);

        [SerializeField]
        private Vector3 lookOffset = new(0f, 1f, 1.5f);

        [SerializeField]
        [Min(0.01f)]
        private float smoothTime = 0.18f;

        private Vector3 velocity;

        public void Configure(
            Transform followTarget,
            Vector3 cameraOffset,
            Vector3 cameraLookOffset,
            float smoothing)
        {
            target = followTarget;
            offset = cameraOffset;
            lookOffset = cameraLookOffset;
            smoothTime = Mathf.Max(0.01f, smoothing);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime);
            transform.rotation = Quaternion.LookRotation(
                target.position + lookOffset - transform.position,
                Vector3.up);
        }
    }
}
