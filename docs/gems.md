# 宝石（スコアアイテム）システム

取るとスコアが加算される宝石を 3 種類（**10 / 50 / 150 点**）用意しました。

## 追加したもの

| 種類 | プレハブ | マテリアル | 色 | スケール | 点数 |
| --- | --- | --- | --- | --- | --- |
| 小 | `Assets/Prefabs/Gems/Gem_10.prefab` | `Assets/mat/gems/gem_10.mat` | 緑（エメラルド） | 0.55 | 10 |
| 中 | `Assets/Prefabs/Gems/Gem_50.prefab` | `Assets/mat/gems/gem_50.mat` | 青（サファイア） | 0.8 | 50 |
| 大 | `Assets/Prefabs/Gems/Gem_150.prefab` | `Assets/mat/gems/gem_150.mat` | ピンク（ルビー） | 1.15 | 150 |

スクリプト:

- `Assets/Scripts/Collectibles/Gem.cs` — 宝石本体。回転・上下の揺れ・取得判定・スコア加算。
- `Assets/Scripts/Collectibles/GemMeshBuilder.cs` — 宝石メッシュ（ブリリアントカット風）をコードで生成。モデルデータ不要。
- `Assets/Scripts/Score/ScoreManager.cs` — スコアの保持と加算。シングルトン。
- `Assets/Scripts/Score/ScoreHud.cs` — 画面上中央にスコアを表示。取得時に `+10` のようなポップアップも出る。UI 未設定なら Canvas ごと自動生成する。

シーン `Assets/Scenes/sunset.unity` には、カメラの正面（海の上）に宝石を 6 個（10 点×3、50 点×2、150 点×1）配置し、
`GameSystems` オブジェクト（`ScoreManager` + `ScoreHud`）を追加しています。

## 使い方

### 1. プレイヤー側の準備（必須）

宝石はトリガーで取得判定をしています。取る側（サーフボード／プレイヤー）に次の設定が必要です。

1. タグを **`Player`** にする（`Gem` の `Collector Tag` で変更可能。空文字にすると何が当たっても取得できる）
2. `Collider` を付ける（`Is Trigger` はどちらでも可）

> 宝石側にはキネマティックな `Rigidbody` が付いているので、プレイヤー側に `Rigidbody` が無くてもトリガーは発火します。

### 2. 宝石を置く

`Assets/Prefabs/Gems/` の中のプレハブをシーンにドラッグするだけです。
点数だけ変えたい場合は、インスペクタで `Grade` を切り替えるか、`Use Custom Score` を ON にして任意の値を入れてください。

### 3. スコアを取得する

```csharp
int score = ScoreManager.Ensure().Score;

// 加算（宝石以外からスコアを足したいとき）
ScoreManager.Add(100);

// 変化を受け取る
ScoreManager.Ensure().ScoreChanged += (total, delta) => Debug.Log($"{total} (+{delta})");
```

`ScoreManager` はシーンに無くても、最初に必要になった時点で自動生成されます。

## Gem コンポーネントの主な設定

| 項目 | 説明 |
| --- | --- |
| `Grade` | Small(10) / Medium(50) / Large(150) |
| `Use Custom Score` / `Custom Score` | ランクを無視して任意点数にする |
| `Collector Tag` | 取得できる側のタグ。既定は `Player`、空文字なら誰でも取得可 |
| `Respawn Delay` | 0 より大きいと、その秒数後に復活する（0 なら取得時に消滅） |
| `Spin Speed` / `Bob Amplitude` / `Bob Speed` | 回転速度と上下の揺れ |
| `Collect Effect` / `Collect Sound` | 取得時のエフェクトと効果音（任意） |
| `On Collected` | 取得時に呼ばれる UnityEvent（引数は加算スコア） |

## 補足

- 現時点のプロジェクトにはプレイヤーの操作スクリプトがまだ無いため、実際に宝石を取るには
  上記「1. プレイヤー側の準備」を行ったオブジェクトが必要です。
- 宝石のメッシュは `GemMeshBuilder` が実行時（およびエディタ上）に生成します。
  形を変えたい場合は `Sides`（面の数）や `Crown Height` などを調整してください。
- メッシュは実行時生成のため、シーンに置いたインスタンスの `Mesh Filter > Mesh` が
  プレハブのオーバーライドとして表示されることがあります（保存されないので無視して問題ありません）。
