using UnityEngine;
using UnityEngine.InputSystem;
using SeaVibe.Environment;

namespace SeaVibe.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    public class SailController : MonoBehaviour
    {
        [Header("Sail Settings")]
        public Transform sailTransform;
        public float maxSailAngle = 60f;
        public float sailRotationSpeed = 30f;
        public float windMultiplier = 50f;

        [Header("Input")]
        public InputAction rotateLeftAction = new InputAction("SailLeft", binding: "<Keyboard>/q");
        public InputAction rotateRightAction = new InputAction("SailRight", binding: "<Keyboard>/e");
        
        private Rigidbody _boatRb;
        private float _currentSailAngle = 0f;
        private float _targetSailAngle = 0f;

        private void Awake()
        {
            _boatRb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            rotateLeftAction.Enable();
            rotateRightAction.Enable();
        }

        private void OnDisable()
        {
            rotateLeftAction.Disable();
            rotateRightAction.Disable();
        }

        private void Update()
        {
            HandleSailInput();
            RotateSail();
        }

        private void FixedUpdate()
        {
            ApplyWindForce();
        }

        private void HandleSailInput()
        {
            if (rotateLeftAction.IsPressed())
            {
                _targetSailAngle -= sailRotationSpeed * Time.deltaTime;
            }
            if (rotateRightAction.IsPressed())
            {
                _targetSailAngle += sailRotationSpeed * Time.deltaTime;
            }

            _targetSailAngle = Mathf.Clamp(_targetSailAngle, -maxSailAngle, maxSailAngle);
        }

        private void RotateSail()
        {
            if (sailTransform != null)
            {
                _currentSailAngle = Mathf.Lerp(_currentSailAngle, _targetSailAngle, Time.deltaTime * 5f);
                sailTransform.localEulerAngles = new Vector3(0, _currentSailAngle, 0);
            }
        }

        private void ApplyWindForce()
        {
            if (WindManager.Instance == null || sailTransform == null) return;

            Vector3 windVector = WindManager.Instance.GetWindVector();
            Vector3 windDir = windVector.normalized;
            
            Vector3 sailNormal = sailTransform.forward; 
            float windAlignment = Vector3.Dot(windDir, sailNormal);
            
            if (windAlignment > 0)
            {
                float forceMagnitude = windAlignment * WindManager.Instance.windStrength * windMultiplier;
                _boatRb.AddForce(transform.forward * forceMagnitude, ForceMode.Force);
            }
        }
    }
}
