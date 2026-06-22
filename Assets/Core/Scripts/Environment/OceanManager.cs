using UnityEngine;

namespace SeaVibe.Environment
{
    public enum OceanState { Calm, Moderate, Rough, Storm }

    public class OceanManager : MonoBehaviour
    {
        public static OceanManager Instance { get; private set; }

        [Header("Mořské profily")]
        public OceanState currentState = OceanState.Calm;

        [System.Serializable]
        public struct Wave
        {
            public float amplitude;
            public float directionAngle;
            public float wavelength;
            public float steepness; // 0 to 1
        }

        [Header("Aktuální vlny (Zobrazují se podle profilu)")]
        public Wave[] waves;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            ApplyOceanState(currentState);
        }

        private void OnValidate()
        {
            ApplyOceanState(currentState);
        }

        public void ApplyOceanState(OceanState state)
        {
            currentState = state;
            switch (state)
            {
                case OceanState.Calm:
                    waves = new Wave[] {
                        new Wave { amplitude = 0.15f, directionAngle = 0f, wavelength = 12f, steepness = 0.4f },
                        new Wave { amplitude = 0.05f, directionAngle = 45f, wavelength = 6f, steepness = 0.2f }
                    };
                    break;
                case OceanState.Moderate:
                    waves = new Wave[] {
                        new Wave { amplitude = 0.8f, directionAngle = 15f, wavelength = 25f, steepness = 0.5f },
                        new Wave { amplitude = 0.3f, directionAngle = -30f, wavelength = 15f, steepness = 0.3f }
                    };
                    break;
                case OceanState.Rough:
                    waves = new Wave[] {
                        new Wave { amplitude = 2.2f, directionAngle = 5f, wavelength = 50f, steepness = 0.6f },
                        new Wave { amplitude = 0.8f, directionAngle = 40f, wavelength = 25f, steepness = 0.4f }
                    };
                    break;
                case OceanState.Storm:
                    waves = new Wave[] {
                        new Wave { amplitude = 4.5f, directionAngle = 0f, wavelength = 90f, steepness = 0.7f },
                        new Wave { amplitude = 1.5f, directionAngle = 25f, wavelength = 40f, steepness = 0.5f }
                    };
                    break;
            }
        }

        // Gerstnerův výpočet vln (posunuje vertexy ve 3D)
        public Vector3 GetWaveDisplacement(Vector3 position)
        {
            Vector3 displacement = Vector3.zero;
            float time = Time.time;

            foreach (var wave in waves)
            {
                float k = 2 * Mathf.PI / wave.wavelength;
                // Disperzní vztah pro hlubokou vodu: c = sqrt(g / k)
                float c = Mathf.Sqrt(9.81f / k); 
                
                float angleRad = wave.directionAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;

                float f = k * (dir.x * position.x + dir.y * position.z) - c * k * time;

                // Horizontální zdrhnutí (dělá špičaté vlny)
                displacement.x += dir.x * (wave.steepness * wave.amplitude) * Mathf.Cos(f);
                displacement.z += dir.y * (wave.steepness * wave.amplitude) * Mathf.Cos(f);
                // Vertikální výška
                displacement.y += wave.amplitude * Mathf.Sin(f);
            }

            return displacement;
        }

        // Zpětná kompatibilita pro starší skripty (lodě apod.), co chtějí jen výšku
        public float GetWaterHeightAt(Vector3 position)
        {
            return transform.position.y + GetWaveDisplacement(position).y;
        }
    }
}
