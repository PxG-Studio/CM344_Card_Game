using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// 3D rotating inverted pyramid indicator that hovers over the active player's panel
    /// </summary>
    public class TurnIndicator3D : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 50f;
        
        [Header("Hover Animation")]
        [SerializeField] private float hoverHeight = 0.3f;
        [SerializeField] private float hoverSpeed = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0f); // Gold/Yellow
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Gray
        
        private Vector3 startPosition;
        private float hoverOffset;
        private MeshRenderer meshRenderer;
        private bool isActive = false;

        private void Start()
        {
            startPosition = transform.localPosition;
            meshRenderer = GetComponent<MeshRenderer>();
            
            // Create the inverted pyramid mesh
            CreateInvertedPyramidMesh();
            
            // Set initial color
            UpdateColor();
        }

        private void Update()
        {
            // Rotate the pyramid continuously
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            
            // Hover up and down
            hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            transform.localPosition = startPosition + new Vector3(0, hoverOffset, 0);
        }

        public void SetActive(bool active)
        {
            isActive = active;
            UpdateColor();
            gameObject.SetActive(active);
        }

        private void UpdateColor()
        {
            if (meshRenderer != null)
            {
                Material mat = meshRenderer.material;
                mat.color = isActive ? activeColor : inactiveColor;
            }
        }

        private void CreateInvertedPyramidMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            Mesh mesh = new Mesh();
            mesh.name = "InvertedPyramid";

            // Define vertices for inverted pyramid (square base at top, point at bottom)
            float size = 0.5f;
            Vector3[] vertices = new Vector3[]
            {
                // Top square base (4 corners)
                new Vector3(-size, size, -size),  // 0: Top-left-back
                new Vector3(size, size, -size),   // 1: Top-right-back
                new Vector3(size, size, size),    // 2: Top-right-front
                new Vector3(-size, size, size),   // 3: Top-left-front
                
                // Bottom point
                new Vector3(0, -size, 0)          // 4: Bottom point
            };

            // Define triangles (each face)
            int[] triangles = new int[]
            {
                // Top face (square base)
                0, 2, 1,
                0, 3, 2,
                
                // Side faces (4 triangular faces)
                0, 1, 4, // Back face
                1, 2, 4, // Right face
                2, 3, 4, // Front face
                3, 0, 4  // Left face
            };

            // Calculate normals for proper lighting
            Vector3[] normals = new Vector3[vertices.Length];
            for (int i = 0; i < 4; i++)
            {
                normals[i] = Vector3.up; // Top face normals point up
            }
            normals[4] = Vector3.down; // Bottom point normal

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;

            // Add MeshRenderer if not present
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            // Create and assign material - use Unlit shader for UI rendering
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = inactiveColor;
            meshRenderer.material = mat;
            
            Debug.Log($"TurnIndicator3D: Created inverted pyramid mesh on {gameObject.name}");
        }
    }
}

