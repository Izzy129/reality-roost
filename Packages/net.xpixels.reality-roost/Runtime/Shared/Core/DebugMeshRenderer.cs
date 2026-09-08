using UnityEngine;

namespace RealityRoost.Shared
{
    /// <summary>
    /// DebugMeshRenderer.cs is an tool to help show the position of the rails in-editor. It will NOT be shown in Play mode.
    /// With this tool, users will visualize their custom railing without entering Play mode.
    /// 
    /// How to use:
    /// 1) Select the GameObject you want to add as your custom railing.
    /// 2) Add the "DebugMeshRenderer" component to your GameObject.
    /// 3) You should see a rendering overlay appear over your GameObject. 
    /// 4) If you do not see the rendering overview:
    ///     a. Ensure that you're looking at the 'Scene' view
    ///     b. Ensure that Gizmos are toggled on. https://docs.unity3d.com/6000.0/Documentation/Manual/GizmosMenu.html
    /// 
    /// How to toggle off:
    /// 1) Select the GameObject 
    /// 2) Uncheck the 'showMesh' boolean
    /// </summary>
    public class DebugMeshRenderer : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        private Mesh mesh;
        [SerializeField] private bool showMesh = true;
        [SerializeField] private Color lineColor = Color.red;
        void OnDrawGizmos()
        {
            if(Application.isPlaying) this.enabled = false;

            if (!showMesh) return;
            meshFilter = gameObject.GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
            Gizmos.color = lineColor;
            DrawMeshLines();
        }
        private void DrawMeshLines()
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i+=3)
            {
                Vector3 a = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 b = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 c = transform.TransformPoint(vertices[triangles[i + 2]]);

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(b, c);
                Gizmos.DrawLine(c, a);
            }
        }
    }
}
