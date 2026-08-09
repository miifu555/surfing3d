using System.Collections.Generic;
using UnityEngine;

namespace Surfing3D.Collectibles
{
    /// <summary>
    /// 宝石（ブリリアントカット風）のメッシュを手続き的に生成する。
    /// モデルデータを用意しなくても宝石の見た目が作れるようにするためのコンポーネント。
    /// フラットシェーディングになるよう、面ごとに頂点を複製している。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu("Surfing3D/Gem Mesh Builder")]
    public class GemMeshBuilder : MonoBehaviour
    {
        [Tooltip("側面の分割数（多いほど丸い宝石になる）")]
        [Range(3, 24)]
        [SerializeField] int m_Sides = 6;

        [Tooltip("ガードル（一番太い部分）の半径")]
        [SerializeField] float m_Radius = 0.5f;

        [Tooltip("上面（テーブル）の半径 = Radius × この比率")]
        [Range(0.05f, 0.95f)]
        [SerializeField] float m_TableRatio = 0.45f;

        [Tooltip("ガードルから上面までの高さ")]
        [SerializeField] float m_CrownHeight = 0.28f;

        [Tooltip("ガードルから下の尖りまでの深さ")]
        [SerializeField] float m_PavilionDepth = 0.62f;

        [Tooltip("ガードルの帯の厚み")]
        [SerializeField] float m_GirdleHeight = 0.06f;

        Mesh m_Mesh;
        bool m_Dirty;

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
            m_Radius = Mathf.Max(0.01f, m_Radius);
            m_CrownHeight = Mathf.Max(0f, m_CrownHeight);
            m_PavilionDepth = Mathf.Max(0.01f, m_PavilionDepth);
            m_GirdleHeight = Mathf.Max(0f, m_GirdleHeight);
            m_Dirty = true;
        }

        void Update()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Rebuild();
            }
        }

        void OnDestroy()
        {
            DisposeMesh();
        }

        /// <summary>メッシュを作り直して MeshFilter に割り当てる。</summary>
        [ContextMenu("Rebuild Mesh")]
        public void Rebuild()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter == null)
            {
                return;
            }

            DisposeMesh();

            m_Mesh = BuildMesh();
            m_Mesh.name = "Gem (generated)";
            m_Mesh.hideFlags = HideFlags.DontSave;

            if (Application.isPlaying)
            {
                filter.mesh = m_Mesh;
            }
            else
            {
                filter.sharedMesh = m_Mesh;
            }
        }

        void DisposeMesh()
        {
            if (m_Mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(m_Mesh);
            }
            else
            {
                DestroyImmediate(m_Mesh);
            }

            m_Mesh = null;
        }

        Mesh BuildMesh()
        {
            int sides = Mathf.Clamp(m_Sides, 3, 24);
            float girdleHalf = m_GirdleHeight * 0.5f;

            // ピボットが宝石の中心に来るように、全体を上下方向へずらす量。
            float centerOffset = (m_PavilionDepth - m_CrownHeight) * 0.5f;

            float tableY = m_CrownHeight + girdleHalf + centerOffset;
            float girdleTopY = girdleHalf + centerOffset;
            float girdleBottomY = -girdleHalf + centerOffset;
            float apexY = -m_PavilionDepth - girdleHalf + centerOffset;

            float tableRadius = m_Radius * m_TableRatio;

            var tableRing = new Vector3[sides];
            var topRing = new Vector3[sides];
            var bottomRing = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = (i / (float)sides) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                tableRing[i] = new Vector3(cos * tableRadius, tableY, sin * tableRadius);
                topRing[i] = new Vector3(cos * m_Radius, girdleTopY, sin * m_Radius);
                bottomRing[i] = new Vector3(cos * m_Radius, girdleBottomY, sin * m_Radius);
            }

            var apex = new Vector3(0f, apexY, 0f);
            var tableCenter = new Vector3(0f, tableY, 0f);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;

                // 上面（テーブル）
                AddTriangle(vertices, triangles, tableCenter, tableRing[next], tableRing[i]);

                // クラウン（上面とガードルの間の斜面）
                AddQuad(vertices, triangles, tableRing[i], tableRing[next], topRing[next], topRing[i]);

                // ガードル（側面の帯）
                AddQuad(vertices, triangles, topRing[i], topRing[next], bottomRing[next], bottomRing[i]);

                // パビリオン（下の尖った部分）
                AddTriangle(vertices, triangles, bottomRing[i], bottomRing[next], apex);
            }

            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                uvs[i] = new Vector2(0.5f + v.x / (m_Radius * 2f), 0.5f + v.z / (m_Radius * 2f));
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, a, c, d);
        }
    }
}
