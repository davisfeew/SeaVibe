using UnityEngine;

namespace SeaVibe.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
        [Header("Steering Settings")]
        public float forwardForce = 40000f; // Zvýšený tah vpřed
        public float reverseForce = 15000f; // Zpětný tah
        public float turnTorque = 500000f; // Brutální kroutící moment pro otáčení
        public float rollTorque = 150000f; // Kroutící moment pro vizuální náklon lodi do zatáčky
        
        [Header("Physics Realism")]
        public float lateralDrag = 2.5f; // Odpor vody proti klouzání lodi do boku
        public float engineOffset = 9f; // Vzdálenost motoru od těžiště (9m vzad pro 20m loď)
        
        [Header("Model Correction")]
        [Tooltip("Změňte na 90, -90 nebo 180, pokud loď jede bokem místo dopředu.")]
        public float forwardAngleOffset = 0f; 
        
        [HideInInspector]
        public bool isSteering = false;
        
        private Rigidbody _rb;
        private Transform _steeringWheel;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _steeringWheel = transform.Find("SteeringWheel");
        }

        private Vector3 GetVisualForward()
        {
            Vector3 baseForward = transform.forward;
            if (_steeringWheel != null)
            {
                baseForward = _steeringWheel.up;
            }
            baseForward.y = 0;
            if (baseForward.sqrMagnitude < 0.001f) baseForward = transform.forward;
            
            // Aplikace korekce směru (pokud by model plul bokem jako krab)
            if (Mathf.Abs(forwardAngleOffset) > 0.1f)
            {
                baseForward = Quaternion.Euler(0, forwardAngleOffset, 0) * baseForward;
            }
            
            return baseForward.normalized;
        }

        private void FixedUpdate()
        {
            ApplyLateralDrag();
            
            if (isSteering)
            {
                HandleInput();
            }
        }
        
        private void HandleInput()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            
            float vertical = 0f;
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) vertical += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) vertical -= 1f;
            
            float horizontal = 0f;
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) horizontal += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) horizontal -= 1f;
            
            Vector3 visualForward = GetVisualForward();
            
            // Aplikace dopředné síly přesně na zádi lodi (řízeno přes engineOffset)
            // Tím se přirozeně zvedá příď lodi, když se přidá plyn.
            Vector3 forcePosition = transform.position - visualForward * engineOffset; 
            
            if (vertical > 0)
            {
                _rb.AddForceAtPosition(visualForward * vertical * forwardForce, forcePosition, ForceMode.Force);
            }
            else if (vertical < 0)
            {
                _rb.AddForceAtPosition(visualForward * vertical * reverseForce, forcePosition, ForceMode.Force);
            }
            
            // Zatáčení - Ignorujeme setrvačnost a váhu lodi, použijeme absolutní rotaci
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                float forwardSpeed = Vector3.Dot(_rb.linearVelocity, visualForward);
                float speedFactor = Mathf.Clamp(Mathf.Abs(forwardSpeed) * 0.1f, 0.5f, 1.0f); 
                float turnDir = forwardSpeed >= -0.1f ? 1f : -1f;
                
                // Cílová rychlost otáčení (rad/s)
                float targetAngularVelocityY = horizontal * turnDir * speedFactor * 0.5f; 
                
                Vector3 currentAngularVel = _rb.angularVelocity;
                // Postupné dosažení cílové rotace pomocí Lerp (aby to nebylo trhané)
                currentAngularVel.y = Mathf.Lerp(currentAngularVel.y, targetAngularVelocityY, Time.fixedDeltaTime * 5f);
                _rb.angularVelocity = currentAngularVel;
                
                // Přidání fyzikálního náklonu (Roll) do zatáčky.
                // Aplikujeme sílu kolem podélné osy lodi. Vztlak lodi (floaters) se s tímto bude prát,
                // což vytvoří přirozený náklon, který se po narovnání kormidla sám zhoupne zpět.
                _rb.AddTorque(visualForward * horizontal * turnDir * speedFactor * rollTorque, ForceMode.Force);
            }
            else
            {
                // Rychlé zastavení rotace po puštění klávesy
                Vector3 currentAngularVel = _rb.angularVelocity;
                currentAngularVel.y = Mathf.Lerp(currentAngularVel.y, 0f, Time.fixedDeltaTime * 5f);
                _rb.angularVelocity = currentAngularVel;
            }
        }
        
        private void ApplyLateralDrag()
        {
            Vector3 visualForward = GetVisualForward();
            Vector3 visualRight = Vector3.Cross(Vector3.up, visualForward).normalized;
            
            // Protisíla proti skluzu do boku 
            float lateralVelocity = Vector3.Dot(_rb.linearVelocity, visualRight);
            Vector3 lateralFrictionForce = visualRight * (-lateralVelocity * lateralDrag * _rb.mass);
            
            _rb.AddForce(lateralFrictionForce, ForceMode.Force);
        }
    }
}
