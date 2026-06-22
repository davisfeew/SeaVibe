using UnityEngine;

namespace SeaVibe.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
        [Header("Steering Settings")]
        public float turnTorque = 1000f;
        
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void Steer(float inputAmount)
        {
            // Můžeme omezit otáčení podle dopředné rychlosti
            float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
            float effectiveness = Mathf.Clamp(Mathf.Abs(forwardSpeed) * 0.5f, 0.1f, 1f); // Aspoň trochu se otočí vždycky
            
            _rb.AddTorque(transform.up * inputAmount * turnTorque * effectiveness, ForceMode.Force);
        }
    }
}
