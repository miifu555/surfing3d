using UnityEngine;

namespace Surfing3D.Collectibles
{
    /// <summary>
    /// 宝石を取ったときに出るエフェクト。
    /// 小さな破片を飛び散らせ、縮みながら消えていく。指定した秒数が経つと自分ごと消える。
    /// パーティクルの用意が要らないよう、破片はコードから生成している。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Gem Collect Effect")]
    public class GemCollectEffect : MonoBehaviour
    {
        [Header("Shards")]
        [Tooltip("飛び散る破片の数")]
        [Range(1, 40)]
        [SerializeField] int m_ShardCount = 12;

        [Tooltip("破片の大きさ")]
        [SerializeField] float m_ShardSize = 0.14f;

        [Tooltip("飛び散る速さの最小値／最大値")]
        [SerializeField] float m_MinSpeed = 2f;

        [SerializeField] float m_MaxSpeed = 4.5f;

        [Tooltip("上方向への打ち上げの強さ")]
        [SerializeField] float m_UpwardBias = 1.2f;

        [Tooltip("落下の強さ")]
        [SerializeField] float m_Gravity = 6f;

        [Tooltip("破片が回る速さ（度/秒）")]
        [SerializeField] float m_SpinSpeed = 360f;

        [Header("Lifetime")]
        [Tooltip("エフェクトが消えるまでの秒数")]
        [Min(0.05f)]
        [SerializeField] float m_Lifetime = 2f;

        [Tooltip("寿命が来たら自分の GameObject を削除する")]
        [SerializeField] bool m_DestroyWhenDone = true;

        [Header("Look")]
        [Tooltip("破片に使うマテリアル（未設定なら標準マテリアルを色だけ変えて使う）")]
        [SerializeField] Material m_ShardMaterial;

        Transform[] m_Shards;
        Renderer[] m_Renderers;
        Vector3[] m_Velocities;
        Vector3[] m_SpinAxes;
        float m_Timer;
        Color m_Color = new Color(1f, 1f, 1f, 1f);

        /// <summary>エフェクトが消えるまでの秒数。</summary>
        public float Lifetime => m_Lifetime;

        /// <summary>破片の色を設定する。生成直後（Start より前）に呼んでもよい。</summary>
        public void SetColor(Color color)
        {
            m_Color = color;
            ApplyColor();
        }

        void Start()
        {
            Build();
        }

        void Update()
        {
            m_Timer += Time.deltaTime;

            float t = Mathf.Clamp01(m_Timer / Mathf.Max(0.05f, m_Lifetime));
            float dt = Time.deltaTime;

            if (m_Shards != null)
            {
                for (int i = 0; i < m_Shards.Length; i++)
                {
                    var shard = m_Shards[i];
                    if (shard == null)
                    {
                        continue;
                    }

                    m_Velocities[i] += Vector3.down * (m_Gravity * dt);
                    shard.position += m_Velocities[i] * dt;
                    shard.Rotate(m_SpinAxes[i], m_SpinSpeed * dt, Space.Self);

                    // 最後まで縮み続けて、消えるころには見えなくなる
                    shard.localScale = Vector3.one * (m_ShardSize * (1f - t));
                }
            }

            if (t >= 1f && m_DestroyWhenDone)
            {
                Destroy(gameObject);
            }
        }

        void Build()
        {
            int count = Mathf.Max(1, m_ShardCount);

            m_Shards = new Transform[count];
            m_Renderers = new Renderer[count];
            m_Velocities = new Vector3[count];
            m_SpinAxes = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Shard";

                // 破片同士や地形とぶつかる必要はないのでコライダーは外す
                var col = shard.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                var tr = shard.transform;
                tr.SetParent(transform, false);
                tr.localPosition = Random.insideUnitSphere * (m_ShardSize * 0.5f);
                tr.localRotation = Random.rotation;
                tr.localScale = Vector3.one * m_ShardSize;

                var direction = (Random.onUnitSphere + Vector3.up * m_UpwardBias).normalized;

                m_Shards[i] = tr;
                m_Renderers[i] = shard.GetComponent<Renderer>();
                m_Velocities[i] = direction * Random.Range(m_MinSpeed, m_MaxSpeed);
                m_SpinAxes[i] = Random.onUnitSphere;

                if (m_ShardMaterial != null)
                {
                    m_Renderers[i].sharedMaterial = m_ShardMaterial;
                }
            }

            ApplyColor();
        }

        void ApplyColor()
        {
            if (m_Renderers == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", m_Color);
            block.SetColor("_Color", m_Color);

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].SetPropertyBlock(block);
                }
            }
        }
    }
}
