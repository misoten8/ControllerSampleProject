using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;




namespace WiimoteApi
{
	public class WiimoteSpeaker : WiimoteData
	{
		// 変数宣言
		private Thread audioThread;     // オーディオクリップスレッド
		private bool enabled;
		private bool muted;
		private Wiimote wm;

		public WiimoteSpeaker(Wiimote Owner) : base(Owner)
		{
		}

		public void Init()
		{
			enabled = true;
			muted = true;

			Owner.SendRegisterWriteRequest(RegisterType.CONTROL, 0xa20009, new byte[] { 0x01 });
			Owner.SendRegisterWriteRequest(RegisterType.CONTROL, 0xa20001, new byte[] { 0x08 });

			byte[] config = new byte[] { 0x00, 0x40, 0x70, 0x17, 0x60, 0x00, 0x00 };
			for (int i = 0; i < 7; i++)
			{
				Owner.SendRegisterWriteRequest(RegisterType.CONTROL, 0xa20001 + i, new byte[] { config[i] });
			}

			Owner.SendRegisterWriteRequest(RegisterType.CONTROL, 0xa20008, new byte[] { 0x01 });
			muted = false;
		}

		private bool _Enabled;
		public bool Enabled
		{
			get
			{
				return _Enabled;
			}
			set
			{
				_Enabled = value;
				byte[] mask = new byte[] { (byte)(_Enabled ? 0x04 : 0x00) };
				Owner.SendWithType(OutputDataType.SPEAKER_ENABLE, mask);
			}
		}

		private bool _Muted
		{
			get
			{
				return _Muted;
			}
			set
			{
				_Muted = value;
				byte[] mask = new byte[] {(byte) (_Muted ? 0x04 : 0x00 ) };
				Owner.SendWithType(OutputDataType.SPEAKER_MUTE, mask);
			}
		}

		private bool IsPlaying
		{
			get
			{
				return audioThread != null && audioThread.IsAlive;
			}
		}

		private void AudioThreadFunc(object buffObj)
		{
			byte[] buffer = (byte[])buffObj;
			MemoryStream stream = new MemoryStream(buffer);
			byte[] chunk = new byte[21];
			int readBytes = 0;

			while ((readBytes = stream.Read(chunk, 1, chunk.Length - 1)) > 0)
			{
				// データサイズ読み込み
				chunk[0] = (byte)(readBytes << 3);

				// 
				if (readBytes < chunk.Length - 1)
				{
					for (int i = readBytes + 1; i < chunk.Length; i++)
					{
						chunk[i] = 127;
					}
				}

				// 一定間隔でデータを送信
				Owner.SendWithType(OutputDataType.SPEAKER_DATA, chunk);
				Thread.Sleep(10);
			}

		}

		public int Play(AudioClip audioClip)
		{
			Init();

			if (IsPlaying)
				return 0;
			byte[] buffer = GetAudioClip(audioClip);
			return _Play( buffer);
		}

		public int _Play(byte[] buffer)
		{
			Init();

			if (IsPlaying)
				return 0;

			audioThread = new Thread(AudioThreadFunc);
			audioThread.IsBackground = true;
			audioThread.Start(buffer);
			return 0;
		}

		private static byte[] GetAudioClip(AudioClip audioClip)
		{
			if (audioClip.channels != 1 || audioClip.frequency != 2000)
			{
				throw new NotSupportedException(string.Format("Only 2000hz mono audio.(channels:{0};frequency:{1};)", audioClip.channels, audioClip.frequency));
			}
			float[] samples = new float[audioClip.samples];
			audioClip.GetData(samples, 0);

			byte[] buffer = new byte[samples.Length];
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = (byte)(((samples[i] + 1) * 255) /2);
			}
			return buffer;
		}

		public override bool InterpretData(byte[] data)
		{
			return false;
		}
	}
}