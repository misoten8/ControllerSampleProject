//=============================================================================
//	タイトル  : wiiリモコン
//	ファイル名: WiiTest.cpp
//	作成者    : AT14A341 戸部俊太
//	作成日    : 2017/9/1
//=============================================================================
//	更新履歴	-2017/9/1
//				V0.01 InitialVersion
//=============================================================================
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using WiimoteApi;
using WiimoteApi.Internal;
using WiimoteApi.Util;
using System.IO;

//=============================================================================
//	スクリプト
//=============================================================================
public class WiiTest : MonoBehaviour
{
	// 変数宣言
	public WiimoteModel wmModel;				// モデル
	private Wiimote wm;							// wiiリモコンハンドル
	private Quaternion initialRotation;			// 角度初期値
	private Vector3 wmpOffset = Vector3.zero;   // ジャイロオフセット
	private bool wmSwing;                       // 振っているかどうか
	private Vector3 wmAccel = Vector3.zero;
	private Vector3 wmAccelOld = Vector3.zero;

	public AudioClip sound;

	// 初期化処理
	void Start()
	{
		initialRotation = wmModel.rot.localRotation;
		wmSwing = false;
	}

	// 更新処理
	void Update()
	{
		// 接続されていなかったら終了
		if (!WiimoteManager.HasWiimote()) { return; }

		// 1Pに保存
		wm = WiimoteManager.Wiimotes[0];
		int ret;
		do
		{
			// リモコンの情報を更新
			ret = wm.ReadWiimoteData();

			if (ret > 0 && wm.current_ext == ExtensionController.MOTIONPLUS)
			{
				Vector3 offset = new Vector3(-wm.MotionPlus.PitchSpeed,
												wm.MotionPlus.YawSpeed,
												wm.MotionPlus.RollSpeed) / 95f; // Divide by 95Hz (average updates per second from wiimote)
				wmpOffset += offset;

				wmModel.rot.Rotate(offset, Space.Self);
			}
		} while (ret > 0);

		// ボタン降下情報
		wmModel.a.enabled = wm.Button.a;
		wmModel.b.enabled = wm.Button.b;
		wmModel.one.enabled = wm.Button.one;
		wmModel.two.enabled = wm.Button.two;
		wmModel.d_up.enabled = wm.Button.d_up;
		wmModel.d_down.enabled = wm.Button.d_down;
		wmModel.d_left.enabled = wm.Button.d_left;
		wmModel.d_right.enabled = wm.Button.d_right;
		wmModel.plus.enabled = wm.Button.plus;
		wmModel.minus.enabled = wm.Button.minus;
		wmModel.home.enabled = wm.Button.home;

		// 加速度取得
		Debug.Log(wmAccel =wm.Accel.GetAccelVector());

		// 振っているか判定
		wmSwing = SwingCheck();

		// Wiiリモコンプラスでなければモデルは回転しない
		if (wm.current_ext != ExtensionController.MOTIONPLUS)
		{
			wmModel.rot.localRotation = initialRotation;
		}

		// モーションプラスがアクティブになっていければ終了
		if (!wm.wmp_attached) { return; }
	}

	//=============================================================================
	//	関数名: void OnGUI()
	//	引数  : なし
	//	戻り値: なし
	//	説明  : DebugGUI
	//=============================================================================
	void OnGUI()
	{
		// 枠
		GUI.Box(new Rect(0, 0, 0, Screen.height), "");

		GUILayout.BeginVertical(GUILayout.Width(300));
		GUILayout.Label("WiimoteFound:" + WiimoteManager.HasWiimote());

		// 接続ボタン
		if (GUILayout.Button("Find Wiimote"))
		{
			WiimoteManager.FindWiimotes();
			//wm = InitMotionPlus(WiimoteManager.Wiimotes[0]);
			wm = InitWiimote(WiimoteManager.Wiimotes[0]);
		}

		// 切断ボタン
		if (GUILayout.Button("Cleanup"))
		{
			Debug.Log("Wiiリモコンの接続を解除しました");
			WiimoteManager.Cleanup(wm);
			wm = null;
		}

		// 接続されていなかったらこれ以降を表示しない
		if (wm == null)
		{
			return;
		}

		// 外部コントローラー確認
		GUILayout.Label("Extension: " + wm.current_ext.ToString() + wm.Type);

		// LEDチェック
		GUILayout.BeginHorizontal();
		for (int i = 0; i < 4; i++)
		{
			if (GUILayout.Button("" + i, GUILayout.Width(300 / 4)))
				wm.SendPlayerLED(i == 0, i == 1, i == 2, i == 3);
		}
		if (GUILayout.Button("LED Reset", GUILayout.Width(300 / 4)))
		{
			wm.SendPlayerLED(false, false, false, false);
		}
		GUILayout.EndHorizontal();

		// 振動
		GUILayout.Label("振動");
		if (GUILayout.Button("ON"))
		{
			wm.RumbleOn = true;
			wm.SendStatusInfoRequest();
		}
		if (GUILayout.Button("OFF"))
		{
			wm.RumbleOn = false;
			wm.SendStatusInfoRequest();
		}

		// 加速度
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("But/Acc", GUILayout.Width(300 / 4)))
			wm.SendDataReportMode(InputDataType.REPORT_BUTTONS);
		if (GUILayout.Button("But/Ext8", GUILayout.Width(300 / 4)))
			wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_EXT8);
		if (GUILayout.Button("B/A/Ext16", GUILayout.Width(300 / 4)))
			wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
		if (GUILayout.Button("Ext21", GUILayout.Width(300 / 4)))
			wm.SendDataReportMode(InputDataType.REPORT_EXT21);
		GUILayout.EndHorizontal();

		// モーションプラス設定
		GUILayout.Label("WMP Attached: " + wm.wmp_attached);
		if (GUILayout.Button("Request Identify WMP"))
			wm.RequestIdentifyWiiMotionPlus();
		if ((wm.wmp_attached || wm.Type == WiimoteType.PROCONTROLLER) && GUILayout.Button("Activate WMP"))
			wm.ActivateWiiMotionPlus();
		if ((wm.current_ext == ExtensionController.MOTIONPLUS ||
			wm.current_ext == ExtensionController.MOTIONPLUS_CLASSIC ||
			wm.current_ext == ExtensionController.MOTIONPLUS_NUNCHUCK) && GUILayout.Button("Deactivate WMP"))
			wm.DeactivateWiiMotionPlus();

		GUILayout.Label("Calibrate Accelerometer");
		GUILayout.BeginHorizontal();
		for (int x = 0; x < 3; x++)
		{
			AccelCalibrationStep step = (AccelCalibrationStep)x;
			if (GUILayout.Button(step.ToString(), GUILayout.Width(100)))
				wm.Accel.CalibrateAccel(step);
		}
		GUILayout.EndHorizontal();
	}

	[System.Serializable]
	public class WiimoteModel
	{
		public Transform rot;
		public Renderer a;
		public Renderer b;
		public Renderer one;
		public Renderer two;
		public Renderer d_up;
		public Renderer d_down;
		public Renderer d_left;
		public Renderer d_right;
		public Renderer plus;
		public Renderer minus;
		public Renderer home;
	}

	//=============================================================================
	//	関数名: public bool SwingCheck()
	//	引数  : なし
	//	戻り値: なし
	//	説明  : Wiiリモコン　振動判定処理
	//=============================================================================
	public bool SwingCheck()
	{
		float ac;
		bool swing = false;

		// 加速度の取得
		Vector3 accel = wm.Accel.GetAccelVector();
		if (accel == wmAccelOld)
		{
			return swing;
		}

		
		ac = Math.Abs(accel.x) + Math.Abs(accel.y) + Math.Abs(accel.z);
		Debug.Log("加速度:" + ac);
		//　振った判定
		if (ac < 1.44f)
		{
			Debug.Log("振った");
			// ↓使えない
			//wm.Speaker.Play(sound);
			swing = true;
			wmAccelOld = accel;
		}
		return swing;
	}

	//=============================================================================
	//	関数名: Wiimote InitWiimote( Wiimote wii )
	//	引数  : なし
	//	戻り値: なし
	//	説明  : Wiiリモコン初期化処理関数
	//=============================================================================
	private Wiimote InitWiimote(Wiimote wii)
	{
		wii.Speaker.Init();

		// Wiiモーションプラスを有効化
		wii.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
		wii.RequestIdentifyWiiMotionPlus();
		wii.ActivateWiiMotionPlus();
		return wii;
	}
}
//=============================================================================
//	end of file
//=============================================================================