using UnityEngine;

namespace Cmis3150.Fps.Player
{
    /// <summary>
    /// First-person CharacterController: walk WASD/arrows, run Shift, jump Space, look mouse.
    /// Requires PlayerInputReader + a child Camera named LookCamera (or assigned).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float runSpeed = 7.5f;
        [SerializeField] float jumpHeight = 1.4f;
        [SerializeField] float gravity = -18f;

        [Header("Look")]
        [SerializeField] Transform lookCamera;
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float pitchMin = -80f;
        [SerializeField] float pitchMax = 80f;

        CharacterController _cc;
        PlayerInputReader _input;
        Vector3 _velocity;
        float _pitch;

        public Transform LookCamera => lookCamera;
        public bool IsGrounded => _cc != null && _cc.isGrounded;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            if (lookCamera == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) lookCamera = cam.transform;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsGameOver)
                return;

            Look();
            Move();
        }

        void Look()
        {
            Vector2 look = _input.Look;
            transform.Rotate(0f, look.x * lookSensitivity, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity, pitchMin, pitchMax);
            if (lookCamera != null)
                lookCamera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void Move()
        {
            Vector2 move = _input.Move;
            Vector3 wish = transform.right * move.x + transform.forward * move.y;
            float speed = _input.SprintHeld ? runSpeed : walkSpeed;

            if (_cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            if (_input.JumpPressed && _cc.isGrounded)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            _velocity.y += gravity * Time.deltaTime;
            Vector3 motion = wish.normalized * speed + Vector3.up * _velocity.y;
            _cc.Move(motion * Time.deltaTime);
        }
    }
}
