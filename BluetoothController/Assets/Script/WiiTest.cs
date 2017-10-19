using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using WiimoteApi;


public class WiiTest : MonoBehaviour
{
	public WiimoteModel wmModel;				// モデル
	private Wiimote wm;							// wiiリモコンハンドル
	private Quaternion initialRotation;			// 角度初期値
	private Vector3 wmpOffset = Vector3.zero;	// ジャイロオフセット

	// 初期化処理
	void Start()
	{
		initialRotation = wmModel.rot.localRotation;
	}

	// 更新処理
	void Update()
	{
		// 接続されていなかったら終了
		if (!WiimoteManager.HasWiimote()) { return; }

		// 1Pを保存
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


		Debug.Log(GetAccelVector());


		// Wiiリモコンプラスでなければモデルは回転しない
		if (wm.current_ext != ExtensionController.MOTIONPLUS)
		{
			wmModel.rot.localRotation = initialRotation;
		}
	}

	// デバッグ
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

	private Vector3 GetAccelVector()
	{
		float accel_x;
		float accel_y;
		float accel_z;

		float[] accel = wm.Accel.GetCalibratedAccelData();
		accel_x = accel[0];
		accel_y = -accel[2];
		accel_z = -accel[1];

		return new Vector3(accel_x, accel_y, accel_z).normalized;
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

	// 振動関数
	public bool Swing()
	{
		bool swing = false;


		return swing;
	}
}
