using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AesirLineage.ui {
    /// <summary>
    /// Every link of a star map as one mesh: a quad per link, with the texture's U running across the
    /// width so a soft falloff texture gives a glowing line. One draw call however many links. The second
    /// UV channel carries the distance along the map from the root and how much the link flows, for the
    /// flow shader (the canvas must pass TexCoord1).
    /// </summary>
    internal sealed class LinkGraphic : MaskableGraphic {
        internal struct Segment {
            public Vector2 From;
            public Vector2 To;
            public Color Color;
            /// <summary>How far <see cref="From"/> is from the root along the links, in canvas units.</summary>
            public float FromDistance;
            /// <summary>1 for links light flows along (both ends taken), 0 for the rest.</summary>
            public float Flow;
        }

        internal readonly List<Segment> Segments = new List<Segment>();
        internal float Width = 3f;
        internal Texture Texture;

        public override Texture mainTexture => Texture != null ? Texture : s_WhiteTexture;

        internal void Rebuild() {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            foreach (Segment segment in Segments) {
                Vector2 along = segment.To - segment.From;
                float length = along.magnitude;
                if (length < 0.01f || segment.Color.a <= 0f) { continue; }
                Vector2 side = new Vector2(-along.y, along.x) / length * (Width * 0.5f);
                Color32 tint = segment.Color * color;
                int first = vh.currentVertCount;
                float start = segment.FromDistance;
                float end = start + length;
                vh.AddVert(Vertex(segment.From - side, tint, new Vector2(0f, 0f), new Vector2(start, segment.Flow)));
                vh.AddVert(Vertex(segment.From + side, tint, new Vector2(1f, 0f), new Vector2(start, segment.Flow)));
                vh.AddVert(Vertex(segment.To + side, tint, new Vector2(1f, 1f), new Vector2(end, segment.Flow)));
                vh.AddVert(Vertex(segment.To - side, tint, new Vector2(0f, 1f), new Vector2(end, segment.Flow)));
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }

        private static UIVertex Vertex(Vector2 position, Color32 color, Vector2 uv0, Vector2 uv1) {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = uv0;
            vertex.uv1 = uv1;
            return vertex;
        }
    }
}
