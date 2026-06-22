using UnityEngine;
using UnityEngine.InputSystem;
using SeaVibe.Player;
using SeaVibe.Boat;

namespace SeaVibe.Interaction
{
    public class SteeringWheel : MonoBehaviour, IInteractable
    {
        public Transform playerStandPosition;
        public BoatController boatController;
        
        [Header("Input")]
        public InputAction steerAction = new InputAction("Steer", binding: "<Gamepad>/leftStick/x");

        private bool _isBeingUsed = false;
        private GameObject _currentPlayer;

        private void Awake()
        {
            steerAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
        }

        private void Update()
        {
            if (_isBeingUsed && boatController != null)
            {
                float steerInput = steerAction.ReadValue<float>();
                boatController.Steer(steerInput);
            }
        }

        public string GetInteractionPrompt()
        {
            return _isBeingUsed ? "Stiskni E pro opuštění kormidla" : "Stiskni E pro kormidlování";
        }

        public void OnInteract(GameObject interactor)
        {
            if (!_isBeingUsed)
            {
                StartSteering(interactor);
            }
            else
            {
                StopSteering();
            }
        }

        private void StartSteering(GameObject player)
        {
            _isBeingUsed = true;
            _currentPlayer = player;

            // Disable player movement
            FirstPersonController fpController = player.GetComponent<FirstPersonController>();
            if (fpController != null)
            {
                fpController.enabled = false;
            }

            // Zafixovat hráče u kormidla
            if (playerStandPosition != null)
            {
                player.transform.position = playerStandPosition.position;
                player.transform.rotation = playerStandPosition.rotation;
            }

            player.transform.SetParent(playerStandPosition);
            
            steerAction.Enable();
        }

        private void StopSteering()
        {
            if (_currentPlayer == null) return;

            _isBeingUsed = false;

            // Znovu umožnit hráči pohyb
            FirstPersonController fpController = _currentPlayer.GetComponent<FirstPersonController>();
            if (fpController != null)
            {
                fpController.enabled = true;
            }

            _currentPlayer.transform.SetParent(transform.root);
            _currentPlayer = null;
            
            steerAction.Disable();
        }
    }
}
