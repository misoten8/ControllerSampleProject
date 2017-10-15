using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using WiimoteApi;


public class WiiTest : MonoBehaviour
{
	public WiimoteModel WmModel;
	private Wiimote Wm; // wiiリモコン
	private Vector3 WmOffset = Vector3.zero;
	private Quaternion InitialRotation;

	public RectTransform[] ir_dots;
	public RectTransform[] ir_bb;
	public RectTransform ir_pointer;

	// 初期化処理
	void Start()
	{
		InitialRotation = WmModel.rot.localRotation;
	}

	// 更新処理
	void Update()
	{
		// 接続されていなかったら終了
		if (!WiimoteManager.HasWiimote()) { return; }

		// 1Pを保存
		Wm = WiimoteManager.Wiimotes[0];

		int ret = Wm.ReadWiimoteData();

		while (ret > 0)
		{
			if (ret > 0 && Wm.current_ext == ExtensionController.MOTIONPLUS)
			{
				Vector3 offset = new Vector3(-Wm.MotionPlus.PitchSpeed, Wm.MotionPlus.YawSpeed, Wm.MotionPlus.RollSpeed) /95.0f;
				WmModel.rot.Rotate(offset, Space.Self);
			}
			ret = Wm.ReadWiimoteData();
		}
		// ボタン降下情報
		WmModel.a.enabled = Wm.Button.a;
		WmModel.b.enabled = Wm.Button.b;
		WmModel.one.enabled = Wm.Button.one;
		WmModel.two.enabled = Wm.Button.two;
		WmModel.d_up.enabled = Wm.Button.d_up;
		WmModel.d_down.enabled = Wm.Button.d_down;
		WmModel.d_left.enabled = Wm.Button.d_left;
		WmModel.d_right.enabled = Wm.Button.d_right;
		WmModel.plus.enabled = Wm.Button.plus;
		WmModel.minus.enabled = Wm.Button.minus;
		WmModel.home.enabled = Wm.Button.home;

		Debug.Log(GetAccelVector());

		// Wiiリモコンプラスでなければモデルは回転しない
		if (Wm.current_ext != ExtensionController.MOTIONPLUS)
		{
			WmModel.rot.localRotation = InitialRotation;
		}

		if (ir_dots.Length < 4)
		{
			return;
		}

		float[,] ir = Wm.Ir.GetProbableSensorBarIR();
		for (int i = 0; i < 2; i++)
		{
			float x = (float)ir[i, 0] / 1023f;
			float y = (float)ir[i, 1] / 767f;
			if (x == -1 || y == -1)
			{
				ir_dots[i].anchorMin = new Vector2(0, 0);
				ir_dots[i].anchorMax = new Vector2(0, 0);
			}

			ir_dots[i].anchorMin = new Vector2(x, y);
			ir_dots[i].anchorMax = new Vector2(x, y);

			if (ir[i, 2] != -1)
			{
				int index = (int)ir[i, 2];
				float xmin = (float)Wm.Ir.ir[index, 3] / 127f;
				float ymin = (float)Wm.Ir.ir[index, 4] / 127f;
				float xmax = (float)Wm.Ir.ir[index, 5] / 127f;
				float ymax = (float)Wm.Ir.ir[index, 6] / 127f;
				ir_bb[i].anchorMin = new Vector2(xmin, ymin);
				ir_bb[i].anchorMax = new Vector2(xmax, ymax);
			}
		}

		float[] pointer = Wm.Ir.GetPointingPosition();
		ir_pointer.anchorMin = new Vector2(pointer[0], pointer[1]);
		ir_pointer.anchorMax = new Vector2(pointer[0], pointer[1]);
	}

	// デバッグ
	void OnGUI()
	{
		// 枠
		GUI.Box(new Rect(0,0,0,Screen.height),"");

		GUILayout.BeginVertical(GUILayout.Width(300));
		GUILayout.Label("WiimoteFound:" + WiimoteManager.HasWiimote());

		// 接続ボタン
		if (GUILayout.Button("Find Wiimote"))
		{
			WiimoteManager.FindWiimotes();
		}

		// 切断ボタン
		if (GUILayout.Button("Cleanup") )
		{
			Debug.Log("Wiiリモコンの接続を解除しました");
			WiimoteManager.Cleanup(Wm);
			Wm = null;
		}

		// 接続されていなかったらこれ以降を表示しない
		if (Wm == null)
		{
			return;
		}

		// 外部コントローラー確認
		GUILayout.Label("Extension: " + Wm.current_ext.ToString());

		// LEDチェック
		GUILayout.BeginHorizontal();
		for (int i = 0; i < 4; i++)
		{
			if (GUILayout.Button("" + i, GUILayout.Width(300 / 4)))
				Wm.SendPlayerLED(i == 0, i == 1, i == 2, i == 3);
		}
		if (GUILayout.Button("LED Reset", GUILayout.Width(300 / 4)))
		{
			Wm.SendPlayerLED(false,false,false,false);
		}
		GUILayout.EndHorizontal();

		// 振動
		GUILayout.Label("振動");
		if (GUILayout.Button("ON"))
		{
			Wm.RumbleOn = true;
			Wm.SendStatusInfoRequest();
		}
		if (GUILayout.Button("OFF"))
		{
			Wm.RumbleOn = false;
			Wm.SendStatusInfoRequest();
		}

		// 加速度
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("But/Acc", GUILayout.Width(300 / 4)))
			Wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL);
		if (GUILayout.Button("But/Ext8", GUILayout.Width(300 / 4)))
			Wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_EXT8);
		if (GUILayout.Button("B/A/Ext16", GUILayout.Width(300 / 4)))
			Wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
		if (GUILayout.Button("Ext21", GUILayout.Width(300 / 4)))
			Wm.SendDataReportMode(InputDataType.REPORT_EXT21);
		GUILayout.EndHorizontal();
	}

	private Vector3 GetAccelVector()
	{
		float accel_x;
		float accel_y;
		float accel_z;

		float[] accel = Wm.Accel.GetCalibratedAccelData();
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
}
