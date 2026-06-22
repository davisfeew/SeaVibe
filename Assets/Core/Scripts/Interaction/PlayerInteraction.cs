using UnityEngine;
using UnityEngine.InputSystem;

namespace SeaVibe.Interaction
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactionDistance = 3f;
        public LayerMask interactableLayer;
        public Camera playerCamera;

        [Header("Input")]
        public InputAction interactAction = new InputAction("Interact", binding: "<Keyboard>/e");

        private IInteractable _currentInteractable;

        private void OnEnable()
        {
            interactAction.Enable();
        }

        private void OnDisable()
        {
            interactAction.Disable();
        }

        private void Update()
        {
            CheckForInteractable();

            if (interactAction.triggered && _currentInteractable != null)
            {
                _currentInteractable.OnInteract(gameObject);
            }
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    _currentInteractable = interactable;
                    return;
                }
            }

            _currentInteractable = null;
        }
    }
}
