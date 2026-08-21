using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EXPmover : MonoBehaviour
{
    public float moveSpeed = 3f; // 移動速度
    private Transform target;    // 追いかけるターゲット
    private bool reached = false;
    public int getpoint = 10; 
    
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
                ScoreMan.getScore(getpoint);
                Destroy(gameObject); // 自分を消す
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
}

