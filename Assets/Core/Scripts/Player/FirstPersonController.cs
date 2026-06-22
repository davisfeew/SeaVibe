using UnityEngine;
using UnityEngine.InputSystem;

namespace SeaVibe.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 6f;
        public float maxVelocityChange = 10f;
        public float jumpHeight = 1.5f;

        [Header("Look")]
        public Transform playerCamera;
        public float mouseSensitivity = 0.1f;
        public float maxLookAngle = 85f;

        [Header("Grounding")]
        public LayerMask groundMask;
        public float groundCheckDistance = 1.1f;

        [Header("Input Actions")]
        public InputAction moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        public InputAction lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        public InputAction jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        public InputAction sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");

        private Rigidbody _rb;
        private float _yaw;
        private float _pitch;
        private bool _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _rb.useGravity = false; // We will apply custom gravity for better control
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Zabraňuje propadnutí skrz rychle se hýbající objekty (jako je naše loď)

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Default WASD bindings for Move
            moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
            sprintAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            lookAction.Disable();
            jumpAction.Disable();
            sprintAction.Disable();
        }

        private void Update()
        {
            HandleLook();
            CheckGrounded();

            if (jumpAction.triggered && _isGrounded)
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            HandleMovement();
            ApplyCustomGravity();
        }

        private void HandleLook()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            
            _yaw = transform.localEulerAngles.y + lookInput.x * mouseSensitivity;
            _pitch -= lookInput.y * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, _yaw, 0);
            if (playerCamera != null)
            {
                playerCamera.localEulerAngles = new Vector3(_pitch, 0, 0);
            }
        }

        private void HandleMovement()
        {
            if (!_isGrounded) return;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);
            targetVelocity = transform.TransformDirection(targetVelocity);
            
            float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            targetVelocity *= currentSpeed;

            Vector3 velocity = _rb.linearVelocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;

            _rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
        }

        private void CheckGrounded()
        {
            _isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
        }

        private void ApplyCustomGravity()
        {
            _rb.AddForce(Physics.gravity * _rb.mass);
        }
    }
}
