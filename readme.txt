//=============================================================================
//	タイトル  : wiiリモコン使い方
//	作成者    : AT14A341 戸部俊太
//	作成日    : 2017/10/26
//=============================================================================

// ペアリング方法
Windowsの場合スタート画面からデバイスBluetoothデバイスを追加を選び、Nintendo Controllerみたいなやつをペアリングする。

// wiiリモコンの取得
WiimoteManager.FindWiimotes();
この関数でBluetooth接続されているWiiリモコンを探し、WiimoteManager.wiimotes[]のなかに情報が保存される。
その後Wiimoteクラスの変数にWiimoteManager.wiimotes[]の一つを入れる。
例：
Wiimote wm;
wm = WiimoteManager.wiimotes[0];

// 加速度の取得方法
まずはWiiリモコンに加速度の使用するためのレポートを送信します。

wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
↑この関数がレポートを送信します。

wm.RequestIdentifyWiiMotionPlus();
wm.ActivateWiiMotionPlus();
↑この二行はモーションプラスを探して有効にしています。

上記三行をWiiリモコン接続後に実行していないと加速度の取得ができません。
（毎フレーム呼び出す必要はない）
上記三行を実行後、更新処理関数などで

Vector3 vec = wm.Accel.GetAccelVector()

Vector3型で加速度が返ってくる

// ボタン情報取得
if( wm.Button.a == true )
{

}
↑現状このように取得するようにしてください

関数は後で作ります