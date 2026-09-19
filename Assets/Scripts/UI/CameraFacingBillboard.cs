using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        private Canvas targetCanvas;
        private Transform cameraTransform;

        private void Awake()
        {
            targetCanvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            if (targetCanvas.enabled == false)
            {
                return;
            }

            if (cameraTransform == null)
            {
                CameraComponent mainCamera = CameraComponent.main;
                if (mainCamera == null)
                {
                    return;
                }

                cameraTransform = mainCamera.transform;
            }

            transform.rotation = cameraTransform.rotation;
        }
    }
}
