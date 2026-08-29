using UnityEngine;

// プレイヤーの移動。Rigidbody に速度を与えて動かし、
// 入力元をキーボード（デバッグ用）と micro:bit で切り替えられる。
[RequireComponent(typeof(Rigidbody))]
public class serialmover : MonoBehaviour
{
    public enum InputMode
    {
        // キーボード。デバッグ用
        Keyboard,
        // micro:bit からのシリアル入力（reciever が受け取ったもの）
        MicroBit,
    }

    [Header("操作方法")]
    public InputMode inputMode = InputMode.MicroBit;
    // micro:bit からまだ一度もデータが来ていないときはキーボードで動かす。
    // 実機を挿していないときにエディタで動かせなくなるのを防ぐ
    public bool fallbackToKeyboard = true;
    // 実行中にキーで切り替える
    public bool allowRuntimeToggle = true;
    public KeyCode toggleKey = KeyCode.F1;

    [Header("micro:bit")]
    // この傾き（度）で最大の旋回になる
    public float microbitFullTiltAngle = 45.0f;
    // これ以下の傾きは無視する（度）。手ぶれ対策
    public float microbitDeadZone = 5.0f;
    // 左右が逆に曲がるときは true
    public bool invertMicrobitSteering = false;

    [Header("速度")]
    // 最高速の係数（実際の最高速 = moveSpeed * 10 units/sec）
    public float moveSpeed = 1.0f;
    public float rorateSpeed = 1.0f;

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

    // 他のスクリプト（MovingRotate など）から今の入力を見るための窓口
    public static serialmover Active { get; private set; }
    public float SteerInput { get { return steerInput; } }
    public bool Shake { get { return shake; } }
    public bool Logo { get { return logo; } }
    public string ButtonMark { get { return bottonMark; } }
    // 実際に使われている入力元。fallback が効くと inputMode とは違うことがある
    public InputMode ActiveInputMode { get { return activeMode; } }

    private Rigidbody rb;

    // -1（左）～ +1（右）。micro:bit のときは傾きに応じて途中の値も入る
    private float steerInput = 0.0f;
    private bool shake = false;
    private bool logo = false;
    private string bottonMark = "N";
    private InputMode activeMode = InputMode.Keyboard;

    // Updateで受け取った入力を保持しておき、FixedUpdateで物理的に適用する
    private float pendingRotateY = 0.0f;
    private float pendingMove = 0.0f;

    void Awake()
    {
        Active = this;
        rb = GetComponent<Rigidbody>();

        // Terrain Colliderをすり抜けないための推奨設定
        rb.isKinematic = false;                 // 物理挙動・衝突応答を有効にする
        rb.useGravity = true;                    // 地形の上に乗せたいならON
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 動きを滑らかに
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 高速移動でのすり抜け防止
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 地形の凹凸で転倒しないように

        // 停止するとPhysXがRigidbodyをスリープさせ、linearVelocityを代入しても目覚めないことがある
        rb.sleepThreshold = 0.0f;
    }

    void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    void Update()
    {
        // ここでは「入力の読み取り」だけを行う（Transformは直接動かさない）

        if (allowRuntimeToggle && Input.GetKeyDown(toggleKey))
        {
            inputMode = (inputMode == InputMode.Keyboard) ? InputMode.MicroBit : InputMode.Keyboard;
        }

        activeMode = ResolveInputMode();

        if (activeMode == InputMode.MicroBit)
        {
            ReadMicroBit();
        }
        else
        {
            ReadKeyboard();
        }

        // 回転量を計算して保持（shake中は回転させない）
        pendingRotateY = (shake == false) ? (steerInput * 10.0f * rorateSpeed / 90.0f) : 0.0f;
    }

    // 実際に使う入力元を決める
    InputMode ResolveInputMode()
    {
        if (inputMode == InputMode.MicroBit && fallbackToKeyboard && reciever.Ready == false)
        {
            // まだ micro:bit から何も届いていないのでキーボードで動かす
            return InputMode.Keyboard;
        }

        return inputMode;
    }

    void ReadKeyboard()
    {
        if (Input.GetKey(KeyCode.A))
        {
            steerInput = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            steerInput = 1.0f;
        }
        else
        {
            steerInput = 0.0f;
        }

        shake = Input.GetKey(KeyCode.X);
        logo = Input.GetKey(KeyCode.E);

        if (Input.GetKey(KeyCode.W))
        {
            bottonMark = "A";
        }
        else if (Input.GetKey(KeyCode.S))
        {
            bottonMark = "B";
        }
        else
        {
            bottonMark = "N";
        }
    }

    void ReadMicroBit()
    {
        // 傾き（roll）を -1～+1 の操作量に直す。
        // キーボードの A / D が -1 / +1 なので、同じ尺度に揃えている
        float roll = reciever.rotate;
        if (invertMicrobitSteering)
        {
            roll = -roll;
        }

        float magnitude = Mathf.Abs(roll);
        if (magnitude <= microbitDeadZone)
        {
            steerInput = 0.0f;
        }
        else
        {
            float range = Mathf.Max(0.01f, microbitFullTiltAngle - microbitDeadZone);
            float amount = Mathf.Clamp01((magnitude - microbitDeadZone) / range);
            steerInput = amount * Mathf.Sign(roll);
        }

        shake = reciever.shake;
        logo = reciever.logo;
        bottonMark = string.IsNullOrEmpty(reciever.botton_mark) ? "N" : reciever.botton_mark;
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
        // 壁からの押し戻しも連続衝突判定(CCD)も効かないため、すり抜けを起こす。
        // linearVelocity を設定すれば物理ソルバが衝突を解決してくれる。
        rb.WakeUp();

        Vector3 desired = rb.rotation * Vector3.forward * pendingMove;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = desired.x;
        velocity.z = desired.z;
        // y（重力による落下）はそのまま残す
        rb.linearVelocity = velocity;
    }

    // 常に前へ加速し続ける。旋回中は上限が下がり、Bボタン（Sキー）の間は後退する
    void UpdateSpeed(float deltaTime)
    {
        // カウントダウン中とゴール後は加速させない
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
        if (bottonMark == "B")
        {
            // Bボタン（Sキー）を押している間だけ減速して後退する
            target = -maxForward * reverseSpeedFactor * corneringLimit;
        }
        else
        {
            // 入力がなくても常に前へ加速し続ける
            target = maxForward * corneringLimit;
        }

        // 速くなる方向なら acceleration、遅くなる方向なら deceleration を使う
        float rate = (Mathf.Abs(target) > Mathf.Abs(currentSpeed)) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * deltaTime);

        // 元コードの符号規約を維持：前進のとき pendingMove は負
        pendingMove = -currentSpeed;
    }
}
