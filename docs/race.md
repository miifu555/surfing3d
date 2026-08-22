# ゴール（スタート／ゴールゲート）と 3 ラップ制

スタート地点のオブジェクトをそのままゴールテープとして使い回す、3 周制のレース進行を実装しました。

## 動きの流れ

| タイミング | テープ | ラップ表示 |
| --- | --- | --- |
| スタート前 | 白いスタートテープが張られている | `LAP 1/3` |
| 1 回目の通過 | テープが切れて消える → レース開始 | `LAP 1/3`（中央に `GO!`） |
| 2 回目の通過（1 周目完了） | 消えたまま | `LAP 2/3` |
| 3 回目の通過（2 周目完了） | **赤いゴールテープとして張り直される** | `LAP 3/3`（中央に `FINAL LAP!`） |
| 4 回目の通過（3 周目完了） | ゴールテープが切れる | `LAP 3/3`（中央に `FINISH!`） |

テープは左右 2 枚に分かれていて、通過すると支柱側を軸に開きながら落ちる演出が入ります。
毎ラップ張り直したい場合は、`RaceGate` の `Show Tape Every Lap` を ON にしてください。

## 追加したもの

- `Assets/Prefabs/Race/StartGoalGate.prefab` — 支柱 2 本＋テープ＋通過判定トリガーのゲート
- `Assets/Scripts/Race/RaceManager.cs` — ラップ数とレース状態（Ready / Racing / Finished）の管理
- `Assets/Scripts/Race/RaceGate.cs` — 通過判定。1 回目はスタート、以降は 1 周としてカウント
- `Assets/Scripts/Race/GoalTape.cs` — テープの表示・非表示と「切れる」演出、スタート／ゴールのマテリアル切り替え
- `Assets/Scripts/Race/RaceHud.cs` — **画面右上**に `LAP 1/3` を表示。中央に `GO!` / `FINAL LAP!` / `FINISH!` も出る
- `Assets/mat/race/` — 支柱・スタートテープ（白）・ゴールテープ（赤）のマテリアル
- `Assets/Scripts/UI/HudCanvas.cs` — スコア HUD とラップ HUD で共有する Canvas 生成ヘルパー
- `Assets/Scripts/Common/TriggerFilter.cs` — タグ判定の共通処理（宝石とゲートで共用）

シーン `sunset.unity` には、カメラ正面の海上にゲートを 1 つ置き、
`GameSystems` に `RaceManager`（3 ラップ）と `RaceHud` を追加しています。

## 使い方

### プレイヤー側の準備（宝石と共通・必須）

1. プレイヤー（サーフボード）のタグを **`Player`** にする
2. `Collider` を付ける

ゲート側にキネマティックな `Rigidbody` が付いているので、プレイヤー側の `Rigidbody` は必須ではありません。
タグを変えたい場合は `RaceGate` の `Player Tag`、空文字にすると何が通っても判定します（動作確認時に便利）。

### ラップ数を変える

`GameSystems` の `RaceManager > Total Laps` を変更してください（既定は 3）。
HUD の表示は自動的に `LAP 1/5` のように追従します。

### スクリプトから使う

```csharp
var race = RaceManager.Ensure();

race.CurrentLap;   // 現在のラップ（スタート前は 0）
race.TotalLaps;    // 総ラップ数
race.State;        // Ready / Racing / Finished
race.IsFinalLap;   // 最終ラップ走行中か

race.LapChanged   += (cur, total) => Debug.Log($"LAP {cur}/{total}");
race.LapCompleted += lap => Debug.Log($"{lap} 周目完了");
race.RaceFinished += () => Debug.Log("ゴール！");

race.ResetRace();  // スタート前の状態に戻す
```

## 主な設定

### RaceGate

| 項目 | 説明 |
| --- | --- |
| `Tape` | 制御するゴールテープ（プレハブ内の `Tape` が設定済み） |
| `Tape Respawn Delay` | 通過後にテープを張り直すまでの秒数 |
| `Show Tape Every Lap` | ON で毎ラップ、OFF（既定）は最終ラップのみ張り直す |
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
| `Lap Format` | 既定は `LAP {0}/{1}`（{0}=現在 / {1}=総ラップ数） |
| `Start / Final Lap / Finish Message` | 中央に出すメッセージ文言 |
| `Create UI If Missing` | ラベル未設定なら右上に自動生成する |
