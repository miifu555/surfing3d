using Surfing3D.Scoring;
using UnityEngine;
using UnityEngine.Events;

namespace Surfing3D.Collectibles
{
    /// <summary>宝石のランク。値がそのまま加算スコアになる。</summary>
    public enum GemGrade
    {
        /// <summary>小さい宝石：10 点</summary>
        Small = 10,

        /// <summary>中くらいの宝石：50 点</summary>
        Medium = 50,

        /// <summary>大きい宝石：150 点</summary>
        Large = 150,
    }

    [System.Serializable]
    public class GemCollectedEvent : UnityEvent<int>
    {
    }

    /// <summary>
    /// 取るとスコアが加算される宝石。
    /// トリガーに入ったコライダーのタグが CollectorTag と一致したら取得扱いになる。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Gem")]
    public class Gem : MonoBehaviour
    {
        [Header("Score")]
        [Tooltip("宝石のランク（Small=10 / Medium=50 / Large=150）")]
        [SerializeField] GemGrade m_Grade = GemGrade.Small;

        [Tooltip("ランクを無視して任意のスコアを使う")]
        [SerializeField] bool m_UseCustomScore = false;

        [Tooltip("UseCustomScore が ON のときに加算されるスコア")]
        [SerializeField] int m_CustomScore = 10;

        [Header("Collect")]
        [Tooltip("取得できるオブジェクトのタグ。空文字なら誰でも取得できる")]
        [SerializeField] string m_CollectorTag = "Player";

        [Tooltip("0 より大きいと、その秒数後に復活する。0 以下なら取得時に消滅")]
        [SerializeField] float m_RespawnDelay = 0f;

        [Header("Motion")]
        [Tooltip("Y 軸の回転速度（度/秒）")]
        [SerializeField] float m_SpinSpeed = 90f;

        [Tooltip("上下に揺れる幅（メートル）")]
        [SerializeField] float m_BobAmplitude = 0.15f;

        [Tooltip("上下に揺れる速さ")]
        [SerializeField] float m_BobSpeed = 1.5f;

        [Header("Feedback")]
        [Tooltip("取得時に生成するエフェクト（任意）")]
        [SerializeField] GameObject m_CollectEffect;

        [Tooltip("取得時に鳴らす効果音（任意）")]
        [SerializeField] AudioClip m_CollectSound;

        [Range(0f, 1f)]
        [SerializeField] float m_CollectVolume = 1f;

        [Tooltip("取得時に呼ばれるイベント。引数は加算されたスコア")]
        [SerializeField] GemCollectedEvent m_OnCollected = new GemCollectedEvent();

        Vector3 m_BasePosition;
        float m_BobPhase;
        bool m_Collected;
        Collider[] m_Colliders;
        Renderer[] m_Renderers;

        /// <summary>この宝石を取ったときに加算されるスコア。</summary>
        public int ScoreValue => m_UseCustomScore ? m_CustomScore : (int)m_Grade;

        /// <summary>取得済みかどうか。</summary>
        public bool IsCollected => m_Collected;

        /// <summary>取得時に呼ばれるイベント。</summary>
        public GemCollectedEvent OnCollected => m_OnCollected;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        void OnValidate()
        {
            m_RespawnDelay = Mathf.Max(0f, m_RespawnDelay);
            m_BobAmplitude = Mathf.Max(0f, m_BobAmplitude);
            m_CustomScore = Mathf.Max(0, m_CustomScore);
        }

        void Awake()
        {
            m_BasePosition = transform.position;
            m_BobPhase = Random.value * Mathf.PI * 2f;
            m_Colliders = GetComponentsInChildren<Collider>(true);
            m_Renderers = GetComponentsInChildren<Renderer>(true);
        }

        void Update()
        {
            if (m_Collected)
            {
                return;
            }

            if (!Mathf.Approximately(m_SpinSpeed, 0f))
            {
                transform.Rotate(Vector3.up, m_SpinSpeed * Time.deltaTime, Space.World);
            }

            if (m_BobAmplitude > 0f)
            {
                float offset = Mathf.Sin(m_BobPhase + Time.time * m_BobSpeed) * m_BobAmplitude;
                transform.position = m_BasePosition + Vector3.up * offset;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Collected || !TriggerFilter.Matches(other, m_CollectorTag))
            {
                return;
            }

            Collect();
        }

        /// <summary>宝石を取得させる（スクリプトから直接呼んでもよい）。</summary>
        public void Collect()
        {
            if (m_Collected)
            {
                return;
            }

            m_Collected = true;

            int value = ScoreValue;
            ScoreManager.Ensure().AddScore(value);
            m_OnCollected?.Invoke(value);

            if (m_CollectSound != null)
            {
                AudioSource.PlayClipAtPoint(m_CollectSound, transform.position, m_CollectVolume);
            }

            if (m_CollectEffect != null)
            {
                Instantiate(m_CollectEffect, transform.position, Quaternion.identity);
            }

            if (m_RespawnDelay > 0f)
            {
                SetActiveVisuals(false);
                Invoke(nameof(Respawn), m_RespawnDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>取得済みの宝石を復活させる。</summary>
        public void Respawn()
        {
            CancelInvoke(nameof(Respawn));
            transform.position = m_BasePosition;
            SetActiveVisuals(true);
            m_Collected = false;
        }

        void SetActiveVisuals(bool value)
        {
            if (m_Renderers != null)
            {
                for (int i = 0; i < m_Renderers.Length; i++)
                {
                    if (m_Renderers[i] != null)
                    {
                        m_Renderers[i].enabled = value;
                    }
                }
            }

            if (m_Colliders != null)
            {
                for (int i = 0; i < m_Colliders.Length; i++)
                {
                    if (m_Colliders[i] != null)
                    {
                        m_Colliders[i].enabled = value;
                    }
                }
            }
        }
    }
}
