using UnityEngine;
using UnityEngine.InputSystem;
using SeaVibe.Player;
using SeaVibe.Boat;

namespace SeaVibe.Interaction
{
    public class SteeringWheel : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public Camera playerCamera;
        public Camera steeringCamera;
        public BoatController boatController;
        
        [Header("Settings")]
        public float interactionDistance = 3f;
        public float mouseSensitivity = 0.2f;
        
        private bool _isSteering = false;
        private FirstPersonController _fpController;
        
        // Kamera rotace (Dron)
        private float _xRotation = 30f; // Dron kouká dolů pod úhlem 30st
        private float _yRotation = 0f;
        public float droneDistance = 12f; // Vzdálenost dronu od lodi zkrácena na 12 metrů

        private void Start()
        {
            if (player != null)
                _fpController = player.GetComponent<FirstPersonController>();
                
            if (steeringCamera != null)
            {
                steeringCamera.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);

            if (!_isSteering)
            {
                if (dist <= interactionDistance)
                {
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        StartSteering();
                    }
                }
            }
            else
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    StopSteering();
                    return;
                }
                
                // Rozhlížení myší během kormidlování (Dron)
                if (Mouse.current != null && steeringCamera != null && boatController != null)
                {
                    Vector2 lookDelta = Mouse.current.delta.ReadValue();
                    _yRotation += lookDelta.x * mouseSensitivity;
                    _xRotation -= lookDelta.y * mouseSensitivity;
                    _xRotation = Mathf.Clamp(_xRotation, 5f, 85f); // Dron nepůjde pod vodu (min 5st) a nepřevrátí se
                    
                    // Vypočítáme pozici dronu na orbitě přímo kolem kormidla
                    Quaternion rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
                    
                    Vector3 targetPivot = transform.position + Vector3.up * 2f; // Střed otáčení 2m nad kormidlem
                    
                    steeringCamera.transform.position = targetPivot - (rotation * Vector3.forward * droneDistance);
                    steeringCamera.transform.rotation = rotation;
                }
            }
        }

        private void OnGUI()
        {
            if (!_isSteering && player != null && Vector3.Distance(transform.position, player.position) <= interactionDistance)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 24;
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(20, 20, 400, 40), "Press E to steer", style);
            }
            else if (_isSteering)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 24;
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(20, 20, 400, 40), "Press E to leave wheel", style);
            }
        }

        private void StartSteering()
        {
            _isSteering = true;
            if (_fpController != null) _fpController.enabled = false;
            
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            
            if (boatController != null) 
            {
                boatController.isSteering = true;
                // Skutečná vizuální příď lodi je směr, kam ukazuje vršek kormidla (kvůli rotaci 90st v setupu)
                Vector3 visualForward = transform.up;
                visualForward.y = 0; // Chceme jen rotaci v rovině
                if (visualForward.sqrMagnitude > 0.001f)
                {
                    _yRotation = Quaternion.LookRotation(visualForward).eulerAngles.y;
                }
                else
                {
                    _yRotation = boatController.transform.eulerAngles.y;
                }
            }
            
            if (steeringCamera != null) 
            {
                steeringCamera.gameObject.SetActive(true);
                // Vynutíme okamžitou aktualizaci pozice, aby kamera neskočila na 1 frame jinam
                Quaternion rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
                Vector3 targetPivot = transform.position + Vector3.up * 2f; // Obíháme přímo kolem kormidla!
                steeringCamera.transform.position = targetPivot - (rotation * Vector3.forward * droneDistance);
                steeringCamera.transform.rotation = rotation;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void StopSteering()
        {
            _isSteering = false;
            if (_fpController != null) _fpController.enabled = true;
            
            if (playerCamera != null) playerCamera.gameObject.SetActive(true);
            if (steeringCamera != null) steeringCamera.gameObject.SetActive(false);
            
            if (boatController != null) boatController.isSteering = false;
        }
    }
}
