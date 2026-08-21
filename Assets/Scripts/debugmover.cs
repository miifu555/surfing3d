using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class debugmover : MonoBehaviour
{
    // 最高速の係数（実際の最高速 = moveSpeed * 10 units/sec）
    public float moveSpeed = 1.0f;
    public float debug_rorate = 0.0f;
    public float rorateSpeed = 1.0f;
    public static bool shake = false;
    public string botton_mark = "N";

    public static bool logo = true;

    [Header("加速・減速")]
    // 加速度（units/sec^2）。小さいほどゆっくり速くなる
    public float acceleration = 5.0f;
    // 減速度（units/sec^2）。キーを離したときや、旋回で上限が下がったときの落ち方
    public float deceleration = 8.0f;
    // 後退の最高速（前進の最高速に対する比率）
    public float reverseSpeedFactor = 0.5f;
    // 旋回を全開にしたとき最高速が何倍になるか。0.5 なら半分まで落ちる
    [Range(0.0f, 1.0f)]
    public float corneringSpeedFactor = 0.5f;

    // 現在の速度（前進が正）。動きの確認用に Inspector へ出している
    public float currentSpeed = 0.0f;

    private Rigidbody rb;

    // Updateで受け取った入力を保持しておき、FixedUpdateで物理的に適用する
    private float pendingRotateY = 0.0f;
    private float pendingMove = 0.0f;
    private float steerInput = 0.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Terrain Colliderをすり抜けないための推奨設定
        rb.isKinematic = false;                 // 物理挙動・衝突応答を有効にする
        rb.useGravity = true;                    // 地形の上に乗せたいならON
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 動きを滑らかに
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 高速移動でのすり抜け防止
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 地形の凹凸で転倒しないように

        // 停止するとPhysXがRigidbodyをスリープさせ、linearVelocityを代入しても目覚めないことがある。
        // MovePosition は強制的に起こすので旧コードでは表面化しなかった。
        rb.sleepThreshold = 0.0f;
    }

    void Update()
    {
        // ここでは「入力の読み取り」だけを行う（Transformは直接動かさない）

        if (Input.GetKey(KeyCode.A))
        {
            debug_rorate = -10.0f * rorateSpeed;
            steerInput = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            debug_rorate = 10.0f * rorateSpeed;
            steerInput = 1.0f;
        }
        else
        {
            debug_rorate = 0.0f;
            steerInput = 0.0f;
        }

        shake = Input.GetKey(KeyCode.X);
        logo = Input.GetKey(KeyCode.E);

        if (Input.GetKey(KeyCode.W))
        {
            botton_mark = "A";
        }
        else if (Input.GetKey(KeyCode.S))
        {
            botton_mark = "B";
        }
        else
        {
            botton_mark = "N";
        }

        // 回転量を計算して保持（shake中は回転させない、元コードの挙動を維持）
        pendingRotateY = (shake == false) ? (debug_rorate / 90.0f) : 0.0f;
    }

    void FixedUpdate()
    {
        // 速度の更新は物理と同じ刻みで行う
        UpdateSpeed(Time.fixedDeltaTime);

        // 回転をRigidbody経由で適用（他の物体との衝突演算と整合させる）
        // カウントダウン中は向きも変えられないようにする
        if (RaceCountdown.raceStarted && pendingRotateY != 0.0f)
        {
            Quaternion deltaRotation = Quaternion.Euler(0.0f, pendingRotateY, 0.0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        // 移動は「速度」で与える。
        // MovePosition() は非キネマティックなRigidbodyでは実質テレポートになり、
        // 壁からの押し戻しも連続衝突判定(CCD)も効かないため、すり抜け（トンネリング）を起こす。
        // linearVelocity を設定すれば物理ソルバが衝突を解決してくれる。
        // スリープしているとどんな速度を入れても動かないので必ず起こす
        rb.WakeUp();

        Vector3 desired = rb.rotation * Vector3.forward * pendingMove;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = desired.x;
        velocity.z = desired.z;
        // y（重力による落下）はそのまま残す
        rb.linearVelocity = velocity;
    }

    // 常に前へ加速し続ける。旋回中は上限が下がり、Sキーを押している間は後退する
    void UpdateSpeed(float deltaTime)
    {
        // カウントダウン中は加速させない（RaceCountdown が無いシーンでは常に true）
        if (RaceCountdown.raceStarted == false)
        {
            currentSpeed = 0.0f;
            pendingMove = 0.0f;
            return;
        }

        float maxForward = moveSpeed * 10.0f;

        // 実際に曲がっているときだけ減速させる（shake中は回転しないので対象外）
        float steerAmount = (shake == false) ? Mathf.Abs(steerInput) : 0.0f;
        float corneringLimit = Mathf.Lerp(1.0f, corneringSpeedFactor, steerAmount);

        float target;
        if (botton_mark == "B")
        {
            // Sキー（micro:bitのBボタン）を押している間だけ減速して後退する
            target = -maxForward * reverseSpeedFactor * corneringLimit;
        }
        else
        {
            // 入力がなくても常に前へ加速し続ける
            target = maxForward * corneringLimit;
        }

        // 速くなる方向なら acceleration、遅くなる方向なら deceleration を使う。
        // 旋回で上限が下がったときも「遅くなる方向」になるので自然に減速する。
        float rate = (Mathf.Abs(target) > Mathf.Abs(currentSpeed)) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * deltaTime);

        // 元コードの符号規約を維持：前進のとき pendingMove は負
        pendingMove = -currentSpeed;
    }
}
