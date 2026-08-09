using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debugmover : MonoBehaviour
{
    // ローカル前方に沿って移動する速度（units/sec）
    public float moveSpeed = 1.0f;
    public float debug_rorate = 0.0f;
    public float rorateSpeed = 1.0f;
    public static bool shake = false;
    public string botton_mark = "N";

    public static bool logo = true;



    void Update()
    {
        // transformを取得
        Transform myTransform = this.transform;

        // ローカル座標を基準に、回転を取得
        Vector3 localAngle = myTransform.localEulerAngles;

        if (Input.GetKey(KeyCode.A))
        {
            debug_rorate = -10.0f*rorateSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            debug_rorate = 10.0f*rorateSpeed;
        }
        else
        {
            debug_rorate = 0.0f;
        }

        if (Input.GetKey(KeyCode.X))
        {
            shake = true;
        }
        else
        {
            shake = false;
        }


        if (shake == false)
        {
            localAngle.y += debug_rorate / 90;
        }

        if (Input.GetKey(KeyCode.X))
        {
            shake = true;
        }
        else
        {
            shake = false;
        }

        if (Input.GetKey(KeyCode.E))
        {
            logo = true;
        }
        else
        {
            logo = false;
        }


        // 回転を適用
        myTransform.localEulerAngles = localAngle;

        // フレームレートに依存しない移動量を計算
        float moveAmount = moveSpeed * Time.deltaTime * 10;

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


        if (botton_mark == "A")
        {
            // ローカル前方に沿って前進
            myTransform.Translate(Vector3.forward * moveAmount, Space.Self);
        }
        else if (botton_mark == "B")
        {
            // ローカル後方に沿って後退
            myTransform.Translate(Vector3.back * moveAmount, Space.Self);
        }
    }
}
