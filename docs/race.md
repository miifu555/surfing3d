# スタート／ゴールゲートと制限時間

スタート地点のオブジェクトをそのままゴールテープとして使い回す、制限時間制のレース進行です。

## 動きの流れ

| タイミング | テープ | 画面 |
| --- | --- | --- |
| スタート前 | 白いスタートテープが張ってある | 右上に `TIME 1:30` |
| 1 回目の通過 | テープが切れて消える → **カウントダウン開始** | 中央に `GO!` |
| その約 1.2 秒後 | **赤いゴールテープとして張り直される** | 残り時間が減っていく |
| 残り 10 秒 | — | 時間表示が赤くなり、中央に `HURRY UP!` |
| 2 回目の通過（ゴール） | ゴールテープが切れる | 中央に `GOAL! +1000`、スコアに **+1000** |
| 時間切れ | そのまま | 中央に `TIME UP!`（ボーナスなし） |

テープは左右 2 枚に分かれていて、通過すると支柱側を軸に開きながら落ちる演出が入ります。

## 追加したもの

- `Assets/Prefabs/Race/StartGoalGate.prefab` — 支柱 2 本＋テープ＋通過判定トリガーのゲート
- `Assets/Scripts/Race/RaceManager.cs` — 制限時間のカウントダウン、ゴールボーナスの加算、状態管理（Ready / Racing / Finished）
- `Assets/Scripts/Race/RaceGate.cs` — 通過判定。1 回目でスタート、2 回目でゴール
- `Assets/Scripts/Race/GoalTape.cs` — テープの表示・非表示と「切れる」演出、スタート／ゴールのマテリアル切り替え
- `Assets/Scripts/Race/RaceHud.cs` — **画面右上**に残り時間を表示。中央に `GO!` / `HURRY UP!` / `GOAL!` / `TIME UP!`
- `Assets/mat/race/` — 支柱・スタートテープ（白）・ゴールテープ（赤）のマテリアル

シーン `sunset.unity` には、カメラ正面の海上にゲートを 1 つ置き、
`GameSystems` に `RaceManager`（制限時間 90 秒 / ゴールボーナス 1000 点）と `RaceHud` を追加しています。

## 使い方

### プレイヤー側の準備（宝石と共通・必須）

1. プレイヤー（サーフボード）のタグを **`Player`** にする
2. `Collider` を付ける

ゲート側にキネマティックな `Rigidbody` が付いているので、プレイヤー側の `Rigidbody` は必須ではありません。
タグを変えたい場合は `RaceGate` の `Player Tag`、空文字にすると何が通っても判定します（動作確認時に便利）。

### 制限時間とボーナスを変える

`GameSystems` の `RaceManager` で設定します。

| 項目 | 既定値 | 説明 |
| --- | --- | --- |
| `Time Limit` | 90 | 制限時間（秒） |
| `Goal Bonus` | 1000 | ゴールしたときに加算されるスコア |
| `Hurry Up Time` | 10 | 残りこの秒数で「残りわずか」表示に切り替わる |

### スクリプトから使う

```csharp
var race = RaceManager.Ensure();

race.RemainingTime;  // 残り秒数
race.TimeLimit;      // 制限時間
race.State;          // Ready / Racing / Finished
race.ReachedGoal;    // ゴールで終わったか（時間切れなら false）

race.TimeChanged  += remaining => Debug.Log($"残り {remaining:F1} 秒");
race.HurryUp      += () => Debug.Log("残りわずか！");
race.RaceFinished += (reachedGoal, bonus) =>
    Debug.Log(reachedGoal ? $"ゴール！ +{bonus}" : "時間切れ");

race.ResetRace();    // スタート前の状態に戻す
```

ゴールボーナスは `RaceManager` が `ScoreManager` に加算するので、宝石のスコアと合算されます。

## 主な設定

### RaceGate

| 項目 | 説明 |
| --- | --- |
| `Tape` | 制御するゴールテープ（プレハブ内の `Tape` が設定済み） |
| `Tape Respawn Delay` | スタート後にゴールテープを張り直すまでの秒数 |
| `Show Goal Tape` | OFF にするとスタート後にテープを張り直さない |
| `Player Tag` | 通過を判定するタグ。既定は `Player` |
| `Retrigger Cooldown` | 連続判定を防ぐ待ち時間（既定 3 秒） |

### GoalTape

| 項目 | 説明 |
| --- | --- |
| `Left Pivot` / `Right Pivot` | テープ左右の回転支点（支柱の位置） |
| `Start Material` / `Goal Material` | スタート時（白）とゴール時（赤）のマテリアル |
| `Break Duration` / `Break Swing Angle` / `Break Drop` | 切れる演出の長さ・開く角度・落ちる距離 |

### RaceHud

| 項目 | 説明 |
| --- | --- |
| `Time Format` | 既定は `TIME {0}`（`{0}` に `1:30` が入る） |
| `Normal Color` / `Hurry Color` | 通常時と残りわずかのときの文字色 |
| `Start / Hurry / Goal / Time Up Message` | 中央に出すメッセージ文言（`Goal Message` の `{0}` にボーナス点） |
| `Create UI If Missing` | ラベル未設定なら右上に自動生成する |
