using UnityEngine;
using UnityEngine.InputSystem;

namespace Cmis3150.Fps.Player
{
    /// <summary>
    /// New Input System bindings defined in code so the project runs without
    /// waiting for Unity to generate a C# class from the .inputactions asset.
    /// A matching PlayerControls.inputactions file is also in Assets/Input for the walkthrough.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        InputActionMap _map;
        InputAction _move;
        InputAction _look;
        InputAction _jump;
        InputAction _sprint;
        InputAction _fire;
        InputAction _nextWeapon;
        InputAction _prevWeapon;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool NextWeaponPressed { get; private set; }
        public bool PrevWeaponPressed { get; private set; }

        void Awake()
        {
            _map = new InputActionMap("Player");

            _move = _map.AddAction("Move", type: InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _look = _map.AddAction("Look", type: InputActionType.Value, binding: "<Mouse>/delta");

            _jump = _map.AddAction("Jump", type: InputActionType.Button, binding: "<Keyboard>/space");
            _sprint = _map.AddAction("Sprint", type: InputActionType.Button, binding: "<Keyboard>/leftShift");
            _sprint.AddBinding("<Keyboard>/rightShift");
            _fire = _map.AddAction("Fire", type: InputActionType.Button, binding: "<Mouse>/leftButton");
            _nextWeapon = _map.AddAction("NextWeapon", type: InputActionType.Button, binding: "<Keyboard>/e");
            _prevWeapon = _map.AddAction("PrevWeapon", type: InputActionType.Button, binding: "<Keyboard>/q");
        }

        void OnEnable() => _map.Enable();
        void OnDisable() => _map.Disable();

        void Update()
        {
            Move = _move.ReadValue<Vector2>();
            Look = _look.ReadValue<Vector2>();
            JumpPressed = _jump.WasPressedThisFrame();
            SprintHeld = _sprint.IsPressed();
            FirePressed = _fire.WasPressedThisFrame();
            NextWeaponPressed = _nextWeapon.WasPressedThisFrame();
            PrevWeaponPressed = _prevWeapon.WasPressedThisFrame();
        }

        void OnDestroy()
        {
            _map?.Dispose();
        }
    }
}
