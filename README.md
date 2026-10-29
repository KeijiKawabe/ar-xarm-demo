# ARコマンドパネル式 xArm7 Pick and Place デモ

Unity、Meta Quest、xArm7を使い、AR上の`START`、`PICK`、`MOVE_A`、`MOVE_B`、`MOVE_C`、`PLACE`ボタンで箱を搬送するデモの実装プロジェクトです。

## 実装済み範囲

- Python製TCP JSON Linesサーバー
- SIMULATIONモードでの全シーケンス実行
- 状態遷移と不正コマンド拒否
- 座標・速度・タイムアウト検証
- STOP、通信切断、FAULT、RESET処理
- xArm Python SDK用アダプタ
- Unity側TCPクライアント、状態コントローラ、UI連携スクリプト
- Quest向けWorld Spaceパネル配置、サンプル経路表示、基準座標変換スクリプト
- 単体テスト
- 実行ログ出力（`logs/xarm_demo.log`）

## まずシミュレーションで動かす

```powershell
cd robot_server
..\tools\run_simulation.ps1
```

別のPowerShellで、Python標準ライブラリだけを使った簡易クライアントを実行します。

```powershell
..\tools\demo_client.ps1
```

期待する流れは`WELCOME`、`START`、`PICK`、`MOVE_A`、`MOVE_B`、`MOVE_C`、`PLACE`、`SUCCESS`です。

## テスト

```powershell
cd robot_server
python -m unittest discover -s tests -v
```

## 実機で動かす場合

1. `robot_server/config/demo.json`の`mode`を`real`に変更する。
2. `robot.ip`をxArm7の固定IPへ変更する。
3. グリッパ型式、TCPオフセット、座標、速度上限を実機に合わせる。
4. `robot_server`からサーバーを起動する。
5. 最初は箱なし、低速、非常停止係員付きで確認する。

実機接続には別途`xarm-python-sdk`が必要です。SDKの呼び出しは`robot_server/app/xarm_adapter.py`に隔離しています。

実機用依存関係は、`robot_server/requirements.txt`に固定版を記載しています。まずシミュレーションで確認し、実機ではSDKとxArm本体の対応を確認してから`mode`を`real`へ変更してください。

## Unity

`unity/README.md`に、XR Origin、World Space Canvas、Questのレーザーポインタ、各ボタンの割り当て、AR基準座標の設定手順を記載しています。スクリプトは`unity/Assets/Scripts/`からUnityプロジェクトの`Assets/Scripts/`へコピーしてください。

Unityの標準UIとQuestコントローラのレーザーポインタ入力を組み合わせる想定です。XR入力方式は使用するUnity/XR Pluginのバージョンに応じてシーン側で設定します。

## 重要な実機注意事項

- サンプル座標はそのまま実機へ投入しない。
- UnityのSTOPは物理非常停止の代替ではない。
- 起動時に自動でHOMEへ移動させない。
- 箱、グリッパ、周囲の安全を確認してからRESETする。
