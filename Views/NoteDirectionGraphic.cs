using UnityEngine;
using UnityEngine.UI;
using HMUI;
namespace AdvancedPractice.Views
{
    // A small native UI graphic: no font glyph dependencies for diagonal arrows.
    public sealed class NoteDirectionGraphic : MaskableGraphic
    {
        public Vector2 Direction;
        public float AngleOffset;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .34f;
            Vector2 center = rectTransform.rect.center;
            if (Direction == Vector2.zero)
            {
                AddVertex(mesh, center);
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8;
                    AddVertex(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * scale * .45f);
                }
                for (int i = 0; i < 16; i++) mesh.AddTriangle(0, (i + 1) % 16 + 1, i + 1);
                return;
            }
            Vector2[] points = { new Vector2(-.25f, -.85f), new Vector2(.25f, -.85f), new Vector2(.25f, 0), new Vector2(-.25f, 0), new Vector2(-.85f, -.05f), new Vector2(.85f, -.05f), new Vector2(0, .9f) };
            Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg - 90 + AngleOffset);
            foreach (var point in points) AddVertex(mesh, center + (Vector2)(rotation * (Vector3)(point * scale)));
            mesh.AddTriangle(0, 3, 2); mesh.AddTriangle(0, 2, 1); mesh.AddTriangle(4, 6, 5);
        }
        private void AddVertex(VertexHelper mesh, Vector2 point)
        {
            // Match ImageView's vertex stream for the native no-glow UI shader.
            // UV1 is the normalized rect position (Y down); UV2 carries curve radius.
            Rect rect = rectTransform.rect;
            var curvedCanvas = canvas != null ? canvas.GetComponentInParent<CurvedCanvasSettings>() : null;
            var vertex = UIVertex.simpleVert;
            vertex.position = point;
            vertex.color = color;
            vertex.uv0 = new Vector4(.5f, .5f, 0, 0);
            vertex.uv1 = new Vector4(Mathf.InverseLerp(rect.xMin, rect.xMax, point.x), 1 - Mathf.InverseLerp(rect.yMin, rect.yMax, point.y), 0, 0);
            vertex.uv2 = new Vector4(curvedCanvas != null ? curvedCanvas.radius : 0, 0, 0, 0);
            vertex.normal = Vector3.zero;
            vertex.tangent = Vector4.zero;
            mesh.AddVert(vertex);
        }
    }
}
