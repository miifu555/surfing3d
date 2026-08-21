using UnityEngine;

public class MovingRotate : MonoBehaviour
{
    [Tooltip("傾きの基準にする座標系。未設定なら親を使う。ボード本体(boad)を入れるのが基本")]
    public Transform reference;

    [Tooltip("傾ける軸。reference のローカル座標系で指定する。左右バンクなら前後軸(0,0,1)")]
    public Vector3 tiltAxis = Vector3.forward;

    [Tooltip("最大の傾き角度（度）。左右が逆に倒れる場合はマイナスにする")]
    public float maxAngle = 25.0f;

    [Tooltip("目標角度へ近づく速さ（度/秒）")]
    public float rotate = 60.0f;

    [Tooltip("この速度で最大の傾きになる。0 にすると停止中でも傾く")]
    public float fullTiltSpeed = 3.0f;

    [Tooltip("回転の中心を何ユニット下にずらすか。0 なら自分の原点まわりに回る")]
    public float pivotOffset = 0.0f;

    private Rigidbody referenceBody;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalPosition;
    private float currentAngle;

    void Start()
    {
        // 基準姿勢を覚えておき、毎フレームここからの相対で角度を決める。
        // RotateAround を積み重ねる方式だと傾きが累積して戻らなくなる。
        baseLocalRotation = transform.localRotation;
        baseLocalPosition = transform.localPosition;

        if (reference == null)
        {
            reference = transform.parent;
        }

        // 速度を見るための Rigidbody（ボード本体側にある想定）
        if (reference != null)
        {
            referenceBody = reference.GetComponentInParent<Rigidbody>();
        }
    }

    // 親（ボード）の移動・旋回が確定したあとに姿勢を決めたいので LateUpdate
    void LateUpdate()
    {
        float input = 0.0f;
        if (Input.GetKey(KeyCode.A))
        {
            input = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            input = 1.0f;
        }

        // 走っているときだけ傾ける。水平方向の速さで傾き量をスケールする
        float speedFactor = 1.0f;
        if (referenceBody != null && fullTiltSpeed > 0.0f)
        {
            Vector3 horizontal = referenceBody.linearVelocity;
            horizontal.y = 0.0f;
            speedFactor = Mathf.Clamp01(horizontal.magnitude / fullTiltSpeed);
        }

        // 目標角へ滑らかに寄せる。キーを離すか止まれば 0（中立）へ自動で戻る
        float target = input * maxAngle * speedFactor;
        currentAngle = Mathf.MoveTowards(currentAngle, target, rotate * Time.deltaTime);

        // 軸は reference の座標系で作る。
        // Vector3.right のようなワールド固定軸で回すと、
        // ボードが Y 軸旋回したときに傾く向きがズレていく（前後にお辞儀するなど）。
        Vector3 axis = (reference != null)
            ? reference.TransformDirection(tiltAxis.normalized)
            : tiltAxis.normalized;

        Quaternion tilt = Quaternion.AngleAxis(currentAngle, axis);

        // 基準姿勢に対して毎フレーム上書きする（累積させない）
        Quaternion baseWorldRotation = (transform.parent != null)
            ? transform.parent.rotation * baseLocalRotation
            : baseLocalRotation;
        transform.rotation = tilt * baseWorldRotation;

        // 回転の中心を下にずらす場合は位置も合わせて動かす
        if (pivotOffset != 0.0f)
        {
            Vector3 baseWorldPosition = (transform.parent != null)
                ? transform.parent.TransformPoint(baseLocalPosition)
                : baseLocalPosition;

            Vector3 down = (reference != null) ? -reference.up : Vector3.down;
            Vector3 pivot = baseWorldPosition + down * pivotOffset;

            transform.position = pivot + tilt * (baseWorldPosition - pivot);
        }
        else if (transform.parent != null)
        {
            transform.localPosition = baseLocalPosition;
        }
    }
}
