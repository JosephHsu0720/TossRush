using UnityEngine;

namespace TossRush
{
    // Small vector icon: no external image or font glyph is required.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeakerIcon : UnityEngine.UI.MaskableGraphic
    {
        bool muted;
        public void SetMuted(bool value) { muted = value; SetVerticesDirty(); }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Quad(mesh, new Vector2(-16,-5), new Vector2(-9,-5), new Vector2(-9,5), new Vector2(-16,5));
            Quad(mesh, new Vector2(-9,-5), new Vector2(0,-12), new Vector2(0,12), new Vector2(-9,5));
            if (muted)
            {
                Line(mesh, new Vector2(6,-7), new Vector2(18,7));
                Line(mesh, new Vector2(6,7), new Vector2(18,-7));
                return;
            }
            foreach (float radius in new[] { 13f, 20f })
            {
                for (int i=0; i<10; i++)
                {
                    float a = Mathf.Deg2Rad * (-52 + i*10.4f), b = Mathf.Deg2Rad * (-52 + (i+1)*10.4f);
                    Line(mesh, new Vector2(-5+Mathf.Cos(a)*radius,Mathf.Sin(a)*radius), new Vector2(-5+Mathf.Cos(b)*radius,Mathf.Sin(b)*radius));
                }
            }
        }

        void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b)
        {
            Vector2 normal = new Vector2(-(b-a).y,(b-a).x).normalized * 1.1f;
            Quad(mesh,a-normal,b-normal,b+normal,a+normal);
        }
        void Quad(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(a,color,Vector2.zero); mesh.AddVert(b,color,Vector2.zero);
            mesh.AddVert(c,color,Vector2.zero); mesh.AddVert(d,color,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
