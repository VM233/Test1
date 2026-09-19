using Test1.Combat.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Test1.Combat.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputSource : MonoBehaviour
    {
        private const string MOVE_ACTION_PATH = "Player/Move";

        [SerializeField]
        private VirtualJoystick joystick;

        [SerializeField]
        private InputActionAsset inputActions;

        private InputAction moveAction;

        public Vector2 Move
        {
            get
            {
                Vector2 input = moveAction.ReadValue<Vector2>();
                if (joystick != null)
                {
                    input += joystick.Direction;
                }

                return Vector2.ClampMagnitude(input, 1f);
            }
        }

        private void Awake()
        {
            ResolveMoveAction();
        }

        private void OnEnable()
        {
            moveAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
        }

        public void Configure(
            VirtualJoystick joystickValue,
            InputActionAsset actionAsset)
        {
            moveAction?.Disable();
            joystick = joystickValue;
            inputActions = actionAsset;
            ResolveMoveAction();

            if (Application.isPlaying && isActiveAndEnabled)
            {
                moveAction.Enable();
            }
        }

        private void ResolveMoveAction()
        {
            if (inputActions == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(PlayerInputSource)} on '{name}' requires an " +
                    $"{nameof(InputActionAsset)}.");
            }

            moveAction = inputActions.FindAction(
                MOVE_ACTION_PATH,
                throwIfNotFound: true);
        }
    }
}
