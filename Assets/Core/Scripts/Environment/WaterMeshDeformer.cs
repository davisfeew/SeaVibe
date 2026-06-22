using UnityEngine;

namespace SeaVibe.Environment
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterMeshDeformer : MonoBehaviour
    {
        [Header("High-Poly Water Mesh")]
        [Tooltip("Počet dílků. Čím vyšší, tím jemnější budou vlny (např. 100).")]
        public int resolution = 100;
        public float size = 10f; // Základní velikost

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Vector3[] _baseVertices; 
        private Vector3[] _displacedVertices; 

        private void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            GenerateGrid();
        }

        private void GenerateGrid()
        {
            _mesh = new Mesh();
            _mesh.name = "Custom Water Grid";

            int numVertices = (resolution + 1) * (resolution + 1);
            _baseVertices = new Vector3[numVertices];
            _displacedVertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[resolution * resolution * 6];

            float step = size / resolution;
            float offset = size / 2f;

            int v = 0;
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    _baseVertices[v] = new Vector3(x * step - offset, 0, z * step - offset);
                    uvs[v] = new Vector2((float)x / resolution, (float)z / resolution);
                    v++;
                }
            }

            int t = 0;
            int vert = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    triangles[t + 0] = vert + 0;
                    triangles[t + 1] = vert + resolution + 1;
                    triangles[t + 2] = vert + 1;
                    triangles[t + 3] = vert + 1;
                    triangles[t + 4] = vert + resolution + 1;
                    triangles[t + 5] = vert + resolution + 2;

                    vert++;
                    t += 6;
                }
                vert++;
            }

            _mesh.vertices = _baseVertices;
            _mesh.triangles = triangles;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();

            _meshFilter.mesh = _mesh;
            
            // Odstranil jsem kód, který nutil průhledný materiál, protože v některých verzích Unity 
            // (např. v URP) to způsobí, že se voda zneviditelní. Teď se použije standardní bílá/šedá barva, 
            // kterou zaručeně uvidíš. Barvu si pak změníme přesně podle tvých představ v Editoru.
        }

        private void Update()
        {
            if (OceanManager.Instance == null) return;

            for (int i = 0; i < _baseVertices.Length; i++)
            {
                // Získáme world pozici neutrálního bodu
                Vector3 worldPos = transform.TransformPoint(_baseVertices[i]);
                
                // Získáme 3D posun od Gerstnerových vln
                Vector3 displacement = OceanManager.Instance.GetWaveDisplacement(worldPos);
                
                // Přičteme posun k základní pozici a převedeme zpět do local space
                Vector3 newWorldPos = worldPos + displacement;
                _displacedVertices[i] = transform.InverseTransformPoint(newWorldPos);
            }

            _mesh.vertices = _displacedVertices;
            _mesh.RecalculateNormals(); 
        }
    }
}
