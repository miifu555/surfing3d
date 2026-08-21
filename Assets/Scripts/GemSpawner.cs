using System.Collections.Generic;
using UnityEngine;

// BoxCollider で囲った範囲の中に、宝石をランダムに配置する。
// BoxCollider は範囲の指定に使うだけなので IsTrigger にしておくとよい。
[RequireComponent(typeof(BoxCollider))]
public class GemSpawner : MonoBehaviour
{
    // 配置する宝石の種類と個数の組
    [System.Serializable]
    public class GemEntry
    {
        public GameObject prefab;
        public int count = 10;
    }

    [Header("配置するもの")]
    public GemEntry[] gems;

    // 生成した宝石をまとめる入れ物をシーンに作る。
    // spawner 自身を親にすると、範囲合わせで変えた Transform のスケールが
    // 宝石に掛かってしまい、潰れた見た目になる。
    public bool groupUnderContainer = true;

    [Header("範囲")]
    // 範囲に使う BoxCollider。未設定なら自分に付いているものを使う
    public BoxCollider area;

    [Header("接地")]
    // 真下に地面を探して、その上に置くか
    public bool snapToGround = true;
    // 地面とみなすレイヤー
    public LayerMask groundLayers = ~0;
    // 地面から何ユニット浮かせるか
    public float heightOffset = 0.5f;
    // 地面が見つからなかった場所には置かない
    public bool skipWhenNoGround = true;

    [Header("間隔")]
    // 宝石どうしがこれより近づかないようにする。0 なら判定しない
    public float minDistance = 1.0f;
    // 1個あたりの配置リトライ回数
    public int maxAttempts = 30;

    // 配置済みの座標。間隔チェックに使う
    private readonly List<Vector3> placed = new List<Vector3>();

    // 生成した宝石をまとめる親（スケール1で作る）
    private Transform container;

    void Start()
    {
        Spawn();
    }

    public void Spawn()
    {
        BoxCollider box = GetArea();
        if (box == null)
        {
            Debug.LogWarning("GemSpawner: 範囲に使う BoxCollider がありません", this);
            return;
        }

        if (gems == null || gems.Length == 0)
        {
            Debug.LogWarning("GemSpawner: 配置する宝石が設定されていません", this);
            return;
        }

        placed.Clear();

        foreach (GemEntry entry in gems)
        {
            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 position;
                if (TryFindPosition(box, out position))
                {
                    // 回転もプレハブに設定されているものをそのまま使う
                    GameObject instance = Instantiate(
                        entry.prefab,
                        position,
                        entry.prefab.transform.rotation,
                        GetContainer());

                    // 大きさは元のプレハブのものを使う。
                    // 親のスケールに引きずられないよう明示的に入れ直す。
                    instance.transform.localScale = entry.prefab.transform.localScale;

                    placed.Add(position);
                }
                else
                {
                    Debug.LogWarning("GemSpawner: '" + entry.prefab.name + "' の置き場所が見つかりませんでした（" + maxAttempts + "回試行）", this);
                }
            }
        }
    }

    // 条件を満たす座標が見つかるまで試す
    bool TryFindPosition(BoxCollider box, out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 point = RandomPointInBox(box);

            if (snapToGround)
            {
                Vector3 groundPoint;
                if (GroundPointBelow(box, point, out groundPoint))
                {
                    point = groundPoint;
                }
                else if (skipWhenNoGround)
                {
                    // 地面が無い場所だったので引き直す
                    continue;
                }
            }

            if (IsTooClose(point))
            {
                continue;
            }

            result = point;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // BoxCollider の内側のランダムな一点をワールド座標で返す。
    // TransformPoint を通すので、Box が回転・スケールされていても正しく収まる。
    Vector3 RandomPointInBox(BoxCollider box)
    {
        Vector3 local = box.center + new Vector3(
            Random.Range(-0.5f, 0.5f) * box.size.x,
            Random.Range(-0.5f, 0.5f) * box.size.y,
            Random.Range(-0.5f, 0.5f) * box.size.z
        );

        return box.transform.TransformPoint(local);
    }

    // 真下に地面を探す。範囲の上端から下端まで撃つ
    bool GroundPointBelow(BoxCollider box, Vector3 point, out Vector3 result)
    {
        float height = box.bounds.size.y;
        Vector3 origin = new Vector3(point.x, box.bounds.max.y, point.z);

        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, height, groundLayers, QueryTriggerInteraction.Ignore))
        {
            result = hit.point + Vector3.up * heightOffset;
            return true;
        }

        result = point;
        return false;
    }

    bool IsTooClose(Vector3 point)
    {
        if (minDistance <= 0.0f)
        {
            return false;
        }

        float sqrMin = minDistance * minDistance;

        for (int i = 0; i < placed.Count; i++)
        {
            if ((placed[i] - point).sqrMagnitude < sqrMin)
            {
                return true;
            }
        }

        return false;
    }

    // 宝石をまとめる親を返す。スケール1のオブジェクトをルートに作るので、
    // 中身の大きさがプレハブのまま保たれる。
    Transform GetContainer()
    {
        if (groupUnderContainer == false)
        {
            return null;
        }

        if (container == null)
        {
            GameObject go = new GameObject(gameObject.name + " Gems");
            container = go.transform;
            container.position = Vector3.zero;
            container.rotation = Quaternion.identity;
            container.localScale = Vector3.one;
        }

        return container;
    }

    BoxCollider GetArea()
    {
        if (area != null)
        {
            return area;
        }

        return GetComponent<BoxCollider>();
    }

    // 選択中に配置範囲をシーンビューへ描く
    void OnDrawGizmosSelected()
    {
        BoxCollider box = GetArea();
        if (box == null)
        {
            return;
        }

        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 1.0f, 1.0f, 0.25f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(0.3f, 1.0f, 1.0f, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
