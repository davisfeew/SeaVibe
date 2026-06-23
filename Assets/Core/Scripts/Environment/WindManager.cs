using UnityEngine;

namespace SeaVibe.Environment
{
    public class WindManager : MonoBehaviour
    {
        [Header("Wind Settings")]
        public Vector3 windDirection = new Vector3(1, 0, 0);
        public float windStrength = 10f;
        
        [Header("Dynamic Wind")]
        public bool isDynamic = true;
        public float directionChangeSpeed = 0.5f;
        public float strengthChangeSpeed = 1f;
        public float minStrength = 2f;
        public float maxStrength = 20f;

        public static WindManager Instance { get; private set; }

        private float _targetStrength;
        private Vector3 _targetDirection;
        private float _timeSinceLastChange;
        private float _nextChangeInterval = 5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            windDirection.Normalize();
            _targetDirection = windDirection;
            _targetStrength = windStrength;
        }

        private void Update()
        {
            if (!isDynamic) return;

            // Měníme vítr v reálném čase podle časovače
            _timeSinceLastChange += Time.deltaTime;
            if (_timeSinceLastChange > _nextChangeInterval)
            {
                _timeSinceLastChange = 0f;
                _nextChangeInterval = Random.Range(5f, 15f); // Další změna za 5 až 15 vteřin
                
                float randomAngle = Random.Range(-45f, 45f);
                _targetDirection = Quaternion.Euler(0, randomAngle, 0) * windDirection;
                _targetStrength = Random.Range(minStrength, maxStrength);
            }

            // Smoothly transition wind
            windDirection = Vector3.Lerp(windDirection, _targetDirection, Time.deltaTime * directionChangeSpeed).normalized;
            windStrength = Mathf.Lerp(windStrength, _targetStrength, Time.deltaTime * strengthChangeSpeed);
        }

        public Vector3 GetWindVector()
        {
            return windDirection * windStrength;
        }
    }
}
