//=============================================================================
//	�^�C�g��  : wii�����R���g����
//	�쐬��    : AT14A341 �˕��r��
//	�쐬��    : 2017/10/26
//=============================================================================

// �y�A�����O���@
Windows�̏ꍇ�X�^�[�g��ʂ���f�o�C�XBluetooth�f�o�C�X��ǉ���I�сANintendo Controller�݂����Ȃ���y�A�����O����B

// wii�����R���̎擾
WiimoteManager.FindWiimotes();
���̊֐���Bluetooth�ڑ�����Ă���Wii�����R����T���AWiimoteManager.wiimotes[]�̂Ȃ��ɏ�񂪕ۑ������B
���̌�Wiimote�N���X�̕ϐ���WiimoteManager.wiimotes[]�̈������B
��F
Wiimote wm;
wm = WiimoteManager.wiimotes[0];

// �����x�̎擾���@
�܂���Wii�����R���ɉ����x�̎g�p���邽�߂̃��|�[�g�𑗐M���܂��B

wm.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
�����̊֐������|�[�g�𑗐M���܂��B

wm.RequestIdentifyWiiMotionPlus();
wm.ActivateWiiMotionPlus();
�����̓�s�̓��[�V�����v���X��T���ėL���ɂ��Ă��܂��B

��L�O�s��Wii�����R���ڑ���Ɏ��s���Ă��Ȃ��Ɖ����x�̎擾���ł��܂���B
�i���t���[���Ăяo���K�v�͂Ȃ��j
��L�O�s�����s��A�X�V�����֐��Ȃǂ�

Vector3 vec = wm.Accel.GetAccelVector()

Vector3�^�ŉ����x���Ԃ��Ă���

// �{�^�����擾
if( wm.Button.a == true )
{

}
�����󂱂̂悤�Ɏ擾����悤�ɂ��Ă�������

�֐��͌�ō��܂�