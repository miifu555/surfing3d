using System.Collections.Generic;
using UnityEngine;

// ゴールしたときに宝石を置き直す役。
// シーンに手で置いた宝石は元の場所へ戻し、
// GemSpawner の範囲は改めてランダムに配置し直す。
public class GemResetter : MonoBehaviour
{
    // シーンに最初から置いてある宝石。GemSpawner が作るものは含まない
    private readonly List<EXPmover> placedGems = new List<EXPmover>();

    void Awake()
    {
        // Awake は全ての Start より先に走るので、
        // この時点で見つかるのは手で置いた宝石だけになる。
        // GemSpawner が生成したものは Start で作られるためここには入らない。
        placedGems.Clear();

        EXPmover[] gems = FindObjectsByType<EXPmover>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < gems.Length; i++)
        {
            placedGems.Add(gems[i]);
        }
    }

    // 宝石をすべて置き直す
    public void ResetAll()
    {
        // 手で置いたものは元の場所へ戻す
        for (int i = 0; i < placedGems.Count; i++)
        {
            if (placedGems[i] == null) continue;
            placedGems[i].ResetGem();
        }

        // ランダム配置のものは座標を引き直して置き直す
        GemSpawner[] spawners = FindObjectsByType<GemSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i].Respawn();
        }
    }

    // 置き直した宝石の数（確認用）
    public int PlacedGemCount
    {
        get { return placedGems.Count; }
    }
}
