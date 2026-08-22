using UnityEngine;

namespace Surfing3D
{
    /// <summary>トリガーに入ってきたコライダーが目的の相手かどうかを判定するヘルパー。</summary>
    public static class TriggerFilter
    {
        /// <summary>
        /// コライダー（またはその Rigidbody）のタグが一致するか。
        /// tag が空文字なら常に true を返す。
        /// </summary>
        public static bool Matches(Collider other, string tag)
        {
            if (other == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(tag))
            {
                return true;
            }

            if (other.CompareTag(tag))
            {
                return true;
            }

            // 子のコライダーで当たった場合に備えて、親の Rigidbody 側のタグも見る
            var body = other.attachedRigidbody;
            return body != null && body.CompareTag(tag);
        }
    }
}
