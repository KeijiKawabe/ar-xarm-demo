# LabDemo UI・軸移動仕様

対象: Unity 6000.3.27f2 / XR Interaction Toolkit 3.3.2 / Quest Link。

## セットアップ

1. このブランチのコードを取得し `unity` をUnity Hubから開く。
2. `Assets/Scenes/LabDemo.unity` を開き、コンパイル完了を待つ。
3. 再生停止中に `Lab Demo > Create Command Panel` を実行する。
4. シーンを保存する。生成された `Assets/LabDemoRay.mat` と `.meta` もGitに追加する。
5. リポジトリの `robot_server` で `python run_server.py` を実行する（初期設定はsimulation）。
6. Quest Linkを開始してUnityのPlayを押す。左右のレーザーでボタンを狙い、トリガーを押して離す。

メニューは専用名のパネルと左右レーザーを再生成する。生成物を手動調整した後の再実行ではその調整が置き換わる。既存XR Origin・カメラ・RobotManagerは再利用する。Undoでシーン変更を戻せる。EventSystemは1つを利用し、他の入力モジュールを無効にしてXR UI Input Moduleを有効にする。
Oculus Touch Controller ProfileをStandalone/Androidで有効にする。パッケージのStarter Assetsを手動インポートする必要はない。既存のカメラ追従設定は維持する。パススルーの追加は今回の変更に含まない。

## UI

- START / PICK / X+ / X- / Y+ / Y- / Z+ / Z- / PLACE / RESET / STOP。
- 接続状態、工程、エラー、サーバーが通知する1回の移動量を表示する。
- パネルは起動時のカメラ前方1.25mに1回配置し、その後空間固定。
- クリック時に即時ロックし、サーバーの完了または拒否通知まで通常ボタンを無効にする。STOPは接続中使用可能。
- コントローラー追跡喪失時はレーザー操作を無効にする。
- 接続エラー後はサーバーを起動し直してUnityのPlayを再開始する。再接続ボタンは今回のUIには置かない。

## 軸移動

START → PICK → 任意回数・任意順の軸操作 → PLACE → RESET。
PICK完了後だけ軸操作を受け付け、移動中の追加操作はBUSYで拒否する。
`MOVE_X_PLUS` / `MOVE_X_MINUS` / `MOVE_Y_PLUS` / `MOVE_Y_MINUS` / `MOVE_Z_PLUS` / `MOVE_Z_MINUS`を使用。
ロボットのベース座標系で現在のTCP位置から1軸だけ移動し、他の座標・姿勢は維持する。
`robot_server/config/demo.json` の `axis_control.step_mm` は10mm、最大25mm、速度30mm/sを初期値とする。変更はサーバー再起動後に反映する。
範囲は既存の `limits.x_mm/y_mm/z_mm`。移動前に現在位置と目標位置を検証し、範囲外は移動せずエラー表示する。同じrequest_idの再送は再実行しない。
STOP・切断後は停止状態となり、HELLOを再送しても解除しない。RESETが必要。
互換性のため旧MOVE_A/B/Cコマンドは残すが、新UIには表示しない。

## PICK / PLACE / RESET

PICKは既存の固定取得位置。PLACEは既存の固定PLACE位置へ移動して開き、PLACE_RETRACTへ退避する。**軸操作後の現在位置でそのまま開く意味ではない。** RESETは既存の開放・HOME復帰。
これらの変更を希望する場合は動作を別途定義する。

## 実機投入前に必要な情報

現在の座標・範囲・グリッパー設定はサンプルである。実測したPICK/PLACE/HOME、TCP、グリッパー開閉値、許容作業範囲、速度を設定する。
TCPの箱形範囲チェックは、リンクや持っている箱の障害物衝突判定・関節限界の事前予測ではない。実機の非常停止と試運転は別途必要。UnityのSTOPは物理非常停止を代替しない。

## 検証

`cd robot_server` → `python -m unittest discover -s tests -v`。
6方向、移動量、境界、SDK位置取得失敗、二重送信、BUSY、STOPタイミング、切断、RESET、旧シーケンスを検証。
Unity EditorとQuest実機はこの開発環境では実行できないため、コンパイル・レーザー操作・表示の実機確認は未実施。
PCでは最初にsimulationでSTART→PICK→各軸→PLACEを確認し、停止中/切断中に通常操作が無効なことと、パネルが頭に追従しないことを確認する。
