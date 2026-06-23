using UnityEngine;

namespace SeaVibe.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoatBuoyancy : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [Tooltip("Přetáhni sem 4 prázdné objekty reprezentující rohy trupu na hladině")]
        public Transform[] floaters;
        
        [Header("Physics Settings")]
        public float floatingPower = 1.5f; // Sníženo z 3.0, aby loď při pádu nevystřelila do vesmíru
        public float waterDrag = 0.99f;
        public float waterAngularDrag = 0.5f;
        public float downwardCoMShift = 2.0f; // Posun těžiště o 2 metry dolů (velmi stabilní kýl)
        public float depthBeforeMaxForce = 1f;

        [Header("Ocean Settings")]
        public float oceanHeight = 0f;

        private Rigidbody _rb;
        private float _defaultAngularDrag;
        private float _defaultLinearDrag;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            _defaultAngularDrag = _rb.angularDamping; 
            _defaultLinearDrag = _rb.linearDamping; 

            if (floaters == null || floaters.Length == 0)
            {
                Debug.LogError("[BoatBuoyancy] CHYBA: Lodi chybí manuální vztlakové body (floaters). Skript se vypíná, jinak by loď padala nebo se chovala nesmyslně. Přidej je v Inspektoru!");
                enabled = false;
            }
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                // Vypočítáme lokální směr, který odpovídá světovému "dolů".
                // Tím zabráníme převrácení lodi na bok i v případě, že byl model v editoru manuálně otočen.
                Vector3 localDown = transform.InverseTransformDirection(Vector3.down);
                _rb.centerOfMass = localDown * downwardCoMShift;
            }
        }

        private void FixedUpdate()
        {
            if (floaters == null || floaters.Length == 0) return;

            int floatersUnderwater = 0;
            float gravity = Mathf.Abs(Physics.gravity.y); 
            float totalBuoyancy = _rb.mass * gravity * floatingPower;
            
            foreach (Transform floater in floaters)
            {
                if (floater == null) continue;

                Vector3 applyPosition = floater.position;

                float waterLevel = GetWaterHeightAt(applyPosition);
                float displacement = waterLevel - applyPosition.y;

                if (displacement > 0)
                {
                    floatersUnderwater++;
                    
                    // 1. Ponoření (0 až 1)
                    float submersionFactor = Mathf.Clamp01(displacement / depthBeforeMaxForce);
                    
                    // 2. VZTLAK (Force)
                    Vector3 buoyancyForce = Vector3.up * (totalBuoyancy / floaters.Length) * submersionFactor;
                    
                    // 3. LOKÁLNÍ ODPOR (Local Velocity Damping) - PŘESNÝ RECEPT PROTI SKÁKÁNÍ
                    Vector3 pointVelocity = _rb.GetPointVelocity(applyPosition);
                    // Voda je hustá! Jakmile se bójka dotkne hladiny, musí brzdit naplno, jinak loď proletí pod vodu a pak vyskočí.
                    Vector3 dampingForce = -pointVelocity * waterDrag * (_rb.mass / floaters.Length);
                    
                    // Snížení odporu ve směru plavby (aby loď mohla jet dopředu a nebyla bržděná vodou jak beton)
                    dampingForce.x *= 0.1f;
                    dampingForce.z *= 0.1f;
                    
                    // APLIKACE OBOU SIL V DANÉM BODĚ
                    _rb.AddForceAtPosition(buoyancyForce + dampingForce, applyPosition, ForceMode.Force);
                }
            }

            // 4. GLOBÁLNÍ ODPOR (Pro dodatečnou stabilizaci rotace)
            if (floatersUnderwater > 0)
            {
                _rb.angularDamping = waterAngularDrag;
            }
            else
            {
                // Ve vzduchu necháme loď padat přirozeně
                _rb.linearDamping = _defaultLinearDrag;
                _rb.angularDamping = _defaultAngularDrag;
            }
        }

        private float GetWaterHeightAt(Vector3 position)
        {
            if (SeaVibe.Environment.OceanManager.Instance != null)
            {
                return SeaVibe.Environment.OceanManager.Instance.GetWaterHeightAt(position);
            }
            return oceanHeight;
        }
    }
}
