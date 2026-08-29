using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EXPmover : MonoBehaviour
{
    public float moveSpeed = 3f; // 移動速度
    private Transform target;    // 追いかけるターゲット
    private bool reached = false;
    public int getpoint = 10;

    [Header("吸い込み")]
    // プレイヤーを吸い寄せ始める距離（ワールド単位）。
    // SphereCollider の radius は Transform のスケールが掛かってしまうため、
    // ここで実距離を指定してスケールを打ち消す。
    // プレハブは radius 0.9 × スケール 0.4 = 0.36 しかなく、
    // シーンに手で置いたもの（radius 3）と差が出ていた。
    // 0以下にすると今のコライダー設定をそのまま使う。
    public float attractRadius = 3.0f;

    [Header("取得時の音")]
    // 取った瞬間に鳴らす音。ここに好きな AudioClip を入れる
    public AudioClip pickupSound;
    [Range(0.0f, 1.0f)]
    public float soundVolume = 1.0f;
    // 連続で取ったときに音が重なりすぎないよう、音程をこの幅でばらつかせる。
    // 0 にすると毎回まったく同じ音になる
    [Range(0.0f, 0.5f)]
    public float pitchVariation = 0.08f;

    [Header("取得時のエフェクト")]
    // 手に入れた瞬間に出すエフェクト
    public GameObject pickupEffectPrefab;
    // エフェクトを出してから消すまでの秒数
    public float effectLifeTime = 2.0f;
    // 宝石の色をエフェクトに反映させる
    public bool tintEffectWithGemColor = true;
    
    // 置き直すときに戻る場所
    private Vector3 homePosition;
    private Quaternion homeRotation;

    void Awake()
    {
        ApplyAttractRadius();

        // GemSpawner が生成したものも、この時点では配置後の座標になっている
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    // 取り直せるように元の場所へ戻す。GemResetter から呼ぶ
    public void ResetGem()
    {
        transform.position = homePosition;
        transform.rotation = homeRotation;
        target = null;
        reached = false;
        gameObject.SetActive(true);
    }

    // 吸い込みの判定をワールド単位の距離に合わせる
    void ApplyAttractRadius()
    {
        if (attractRadius <= 0.0f) return;

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = GetComponentInChildren<SphereCollider>();
        }
        if (sphere == null) return;

        Vector3 scale = sphere.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        if (maxScale <= 0.0001f) return;

        sphere.radius = attractRadius / maxScale;
    }

    void Update()
    {
        if (target != null && !reached)
        {
            // ターゲットへ移動
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            // ターゲットに到達したか判定
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < 0.01f)
            {
                reached = true;
                SpawnPickupEffect();
                SoundPlayer.Play(pickupSound, soundVolume, pitchVariation);
                ScoreMan.getScore(getpoint);
                // 破棄せず隠すだけにする。ゴールのたびに置き直せるようにするため
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform;
        }
    }

    // 取得した場所にエフェクトを出し、決めた秒数で消す
    void SpawnPickupEffect()
    {
        if (pickupEffectPrefab == null) return;

        GameObject effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);

        if (tintEffectWithGemColor)
        {
            ApplyGemColor(effect);
        }

        // 宝石本体はすぐ消えるので、エフェクトは親を持たせずに置いておく
        Destroy(effect, effectLifeTime);
    }

    // 宝石のマテリアルの色をパーティクルに移す
    void ApplyGemColor(GameObject effect)
    {
        Renderer gemRenderer = GetComponentInChildren<Renderer>();
        if (gemRenderer == null) return;

        Material material = gemRenderer.sharedMaterial;
        if (material == null) return;

        Color color;
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            color = material.GetColor("_Color");
        }
        else
        {
            return;
        }

        color.a = 1.0f;

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.startColor = color;
        }
    }
}

