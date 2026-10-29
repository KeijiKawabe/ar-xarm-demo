# Unity / Meta Quest セットアップ

## 1. Unityプロジェクト

Unity 2021 LTS以降の3Dプロジェクトに、`Assets/Scripts/`をコピーします。Quest向けにAndroidビルドを行う場合は、使用するUnityバージョンに対応したXR Plugin ManagementとOpenXRを有効にしてください。

## 2. シーン構成

1. XR Origin（または使用するQuestカメラリグ）を配置し、Main Cameraを用意する。
2. 空のGameObjectに`RobotTcpClient`と`RobotDemoController`を追加する。
3. `RobotDemoController.client`へ同じGameObjectの`RobotTcpClient`を割り当てる。
4. World Space Canvasを作り、Canvasのルートへ`WorldSpacePanelAnchor`を追加する。
5. Canvas上に状態表示用のTextと、START/PICK/MOVE_A/MOVE_B/MOVE_C/PLACE/RESET/STOPのButtonを配置する。
6. Canvasの別GameObjectへ`CommandPanelUI`を追加し、各ButtonとTextを割り当てる。
7. 各ButtonのOnClickへ、`RobotDemoController.StartDemo`、`Pick`、`MoveA`、`MoveB`、`MoveC`、`Place`、`Reset`、`Stop`を割り当てる。
8. XR Interaction ToolkitのXR Ray InteractorとXR UI Input Moduleを使い、Questコントローラのトリガーで標準Buttonを押せるようにする。

## 3. AR表示とキャリブレーション

`DemoCoordinateVisualizer`をロボット基準位置のGameObjectへ追加し、`robotFrame`を同じ基準Transformへ割り当てます。`robotFrame`の位置・回転を、デモ台に置いた基準マーカーと実機ベースの測定結果に合わせて変更してください。サンプル座標はxArmのmmをUnityのmへ変換して表示します。

実機にサンプル値をそのまま送らないでください。PICKとPLACEを箱なし・低速で個別確認し、実測した座標をPython側の`robot_server/config/demo.json`へ反映します。

## 4. 接続設定

`RobotTcpClient.host`は、Pythonサーバーを同じPCで動かす場合は`127.0.0.1`、Quest単体アプリからPCへ接続する場合は展示用PCの固定IPv4アドレスに変更します。ポートはPython側と同じ`5000`です。

接続後は自動的にHELLOを送信します。サーバーからREADYを受信するとSTARTが有効になり、完了イベントを受信するたびに次のボタンが有効になります。

## 5. Quest入力の注意

この実装はUnity標準Buttonに依存し、Quest固有の入力APIを直接呼びません。そのため、XR PluginやUnityのバージョンが変わっても、シーン側のXR UI Input Moduleを差し替えて利用できます。STOPは物理非常停止の代替ではありません。
