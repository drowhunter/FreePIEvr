extern "C"
{
  #include "../include/ovr_freepie.h"
}
#include <openvr.h>
#include "PathToolsExcerpt.h"

vr::IVRSystem* m_system;

vr::VRActionSetHandle_t m_actionSet;

vr::VRActionHandle_t m_leftHandPose;
vr::VRActionHandle_t m_rightHandPose;
vr::VRActionHandle_t m_leftTrigger;
vr::VRActionHandle_t m_rightTrigger;
vr::VRActionHandle_t m_leftGrip;
vr::VRActionHandle_t m_rightGrip;
vr::VRActionHandle_t m_leftAnalog;
vr::VRActionHandle_t m_leftAnalogTouch;
vr::VRActionHandle_t m_leftAnalogClick;
vr::VRActionHandle_t m_rightAnalog;
vr::VRActionHandle_t m_rightAnalogTouch;
vr::VRActionHandle_t m_rightAnalogClick;
vr::VRActionHandle_t m_leftXTouch;
vr::VRActionHandle_t m_leftXClick;
vr::VRActionHandle_t m_leftYTouch;
vr::VRActionHandle_t m_leftYClick;
vr::VRActionHandle_t m_rightATouch;
vr::VRActionHandle_t m_rightAClick;
vr::VRActionHandle_t m_rightBTouch;
vr::VRActionHandle_t m_rightBClick;
vr::VRActionHandle_t m_leftThumbRestTouch;
vr::VRActionHandle_t m_rightThumbRestTouch;
vr::VRActionHandle_t m_leftHaptic;
vr::VRActionHandle_t m_rightHaptic;

double	HmdFrameTiming;

int ovr_freepie_init()
{
	if (vr::VR_IsHmdPresent() == false)
		return 1;

	vr::EVRInitError initError = vr::VRInitError_None;
	m_system = vr::VR_Init(&initError, vr::VRApplication_Other);
	if (initError != vr::VRInitError_None)
		return 2;	

	std::string manifestFileName = Path_MakeAbsolute("actions/actions.json", Path_StripFilename(Path_GetExecutablePath()));
	vr::EVRInputError inputError = vr::VRInput()->SetActionManifestPath(manifestFileName.c_str());
	if (inputError != vr::VRInputError_None)
		return 3;

	inputError = vr::VRInput()->GetActionSetHandle("/actions/freepie", &m_actionSet);
	if (inputError != vr::VRInputError_None)
		return 4;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/pose_left", &m_leftHandPose);
	if (inputError != vr::VRInputError_None)
		return 100;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/pose_right", &m_rightHandPose);
	if (inputError != vr::VRInputError_None)
		return 101;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/trigger_left", &m_leftTrigger);
	if (inputError != vr::VRInputError_None)
		return 102;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/trigger_right", &m_rightTrigger);
	if (inputError != vr::VRInputError_None)
		return 103;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/grip_left", &m_leftGrip);
	if (inputError != vr::VRInputError_None)
		return 104;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/grip_right", &m_rightGrip);
	if (inputError != vr::VRInputError_None)
		return 105;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_left", &m_leftAnalog);
	if (inputError != vr::VRInputError_None)
		return 106;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_touch_left", &m_leftAnalogTouch);
	if (inputError != vr::VRInputError_None)
		return 107;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_click_left", &m_leftAnalogClick);
	if (inputError != vr::VRInputError_None)
		return 108;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_right", &m_rightAnalog);
	if (inputError != vr::VRInputError_None)
		return 109;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_touch_right", &m_rightAnalogTouch);
	if (inputError != vr::VRInputError_None)
		return 110;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/analog_click_right", &m_rightAnalogClick);
	if (inputError != vr::VRInputError_None)
		return 111;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_x", &m_leftXTouch);
	if (inputError != vr::VRInputError_None)
		return 112;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_x", &m_leftXClick);
	if (inputError != vr::VRInputError_None)
		return 113;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_y", &m_leftYTouch);
	if (inputError != vr::VRInputError_None)
		return 114;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_y", &m_leftYClick);
	if (inputError != vr::VRInputError_None)
		return 115;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_a", &m_rightATouch);
	if (inputError != vr::VRInputError_None)
		return 116;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_a", &m_rightAClick);
	if (inputError != vr::VRInputError_None)
		return 117;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_b", &m_rightBTouch);
	if (inputError != vr::VRInputError_None)
		return 118;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_b", &m_rightBClick);
	if (inputError != vr::VRInputError_None)
		return 119;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_thumb_rest_left", &m_leftThumbRestTouch);
	if (inputError != vr::VRInputError_None)
		return 120;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/in/button_touch_thumb_rest_right", &m_rightThumbRestTouch);
	if (inputError != vr::VRInputError_None)
		return 121;

	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/out/haptic_left", &m_leftHaptic);
	if (inputError != vr::VRInputError_None)
		return 122;
	inputError = vr::VRInput()->GetActionHandle("/actions/freepie/out/haptic_right", &m_rightHaptic);
	if (inputError != vr::VRInputError_None)
		return 123;

	return 0;
}

int ovr_freepie_reset_orientation()
{
	//ovr_RecenterTrackingOrigin(HMD);
	return 0;
}

int ovr_freepie_setPose(vr::TrackedDevicePose_t* pose, ovr_freepie_6dof *dof)
{
	vr::HmdMatrix34_t matrix = pose->mDeviceToAbsoluteTracking;

	dof->left[0] = matrix.m[0][0];
	dof->left[1] = matrix.m[1][0];
	dof->left[2] = matrix.m[2][0];

	dof->up[0] = matrix.m[0][1];
	dof->up[1] = matrix.m[1][1];
	dof->up[2] = matrix.m[2][1];

	dof->forward[0] = matrix.m[0][2];
	dof->forward[1] = matrix.m[1][2];
	dof->forward[2] = matrix.m[2][2];

	dof->position[0] = matrix.m[0][3];
	dof->position[1] = matrix.m[1][3];
	dof->position[2] = matrix.m[2][3];
	return  0;
}

int ovr_freepie_read(ovr_freepie_data *output)
{
	// get head pose
	vr::TrackedDevicePose_t headTrackedDevicePose;
	m_system->GetDeviceToAbsoluteTrackingPose(vr::TrackingUniverseStanding, 0, &headTrackedDevicePose, 1);
	ovr_freepie_setPose(&headTrackedDevicePose, &output->head);
	output->StatusHead = headTrackedDevicePose.eTrackingResult == vr::TrackingResult_Running_OK ? 2 : headTrackedDevicePose.eTrackingResult > vr::TrackingResult_Running_OK ? 1 : 0;
	output->HmdMounted = m_system->GetTrackedDeviceActivityLevel(0) == vr::k_EDeviceActivityLevel_UserInteraction;

	// update input actions
	vr::VRActiveActionSet_t actionSet = { 0 };
	actionSet.ulActionSet = m_actionSet;
	vr::EVRInputError inputError = vr::VRInput()->UpdateActionState(&actionSet, sizeof(actionSet), 1);
	if (inputError != vr::VRInputError_None)
		return 4;

	vr::InputPoseActionData_t poseData;
	vr::InputAnalogActionData_t analogData;
	vr::InputDigitalActionData_t digitalTouchAction, digitalClickAction;



	inputError = vr::VRInput()->GetDigitalActionData(m_rightATouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 116;
	inputError = vr::VRInput()->GetDigitalActionData(m_rightAClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 117;
	if (digitalClickAction.bState)
	{
		output->A = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->A = 0.5f;
	}
	else
	{
		output->A = 0.0f;
	}


	// get poses
	inputError = vr::VRInput()->GetPoseActionDataForNextFrame(m_leftHandPose, vr::TrackingUniverseStanding, &poseData, sizeof(poseData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 100;
	ovr_freepie_setPose(&poseData.pose, &output->leftHand);
	output->StatusLeftHand = poseData.pose.eTrackingResult == vr::TrackingResult_Running_OK ? 2 : poseData.pose.eTrackingResult > vr::TrackingResult_Running_OK ? 1 : 0;

	inputError = vr::VRInput()->GetPoseActionDataForNextFrame(m_rightHandPose, vr::TrackingUniverseStanding, &poseData, sizeof(poseData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 101;
	ovr_freepie_setPose(&poseData.pose, &output->rightHand);
	output->StatusRightHand = poseData.pose.eTrackingResult == vr::TrackingResult_Running_OK ? 2 : poseData.pose.eTrackingResult > vr::TrackingResult_Running_OK ? 1 : 0;

	// get trigger and grip
	inputError = vr::VRInput()->GetAnalogActionData(m_leftTrigger, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 102;
	output->LeftTrigger = analogData.x;
	
	inputError = vr::VRInput()->GetAnalogActionData(m_rightTrigger, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 103;
	output->RightTrigger = analogData.x;

	inputError = vr::VRInput()->GetAnalogActionData(m_leftGrip, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 104;
	output->LeftGrip = analogData.x;

	inputError = vr::VRInput()->GetAnalogActionData(m_rightGrip, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 105;
	output->RightGrip = analogData.x;

	// get sticks
	inputError = vr::VRInput()->GetAnalogActionData(m_leftAnalog, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 106;
	output->LeftStickAxes[0] = analogData.x;
	output->LeftStickAxes[1] = analogData.y;

	inputError = vr::VRInput()->GetDigitalActionData(m_leftAnalogTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 107;
	inputError = vr::VRInput()->GetDigitalActionData(m_leftAnalogClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 108;
	if (digitalClickAction.bState)
	{
		output->LeftStick = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->LeftStick = 0.5f;
	}
	else
	{
		output->LeftStick = 0.0f;
	}
	
	inputError = vr::VRInput()->GetAnalogActionData(m_rightAnalog, &analogData, sizeof(analogData), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 109;
	output->RightStickAxes[0] = analogData.x;
	output->RightStickAxes[1] = analogData.y;

	inputError = vr::VRInput()->GetDigitalActionData(m_rightAnalogTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 110;
	inputError = vr::VRInput()->GetDigitalActionData(m_rightAnalogClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 111;
	if (digitalClickAction.bState)
	{
		output->RightStick = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->RightStick = 0.5f;
	}
	else
	{
		output->RightStick = 0.0f;
	}

	// get buttons
	inputError = vr::VRInput()->GetDigitalActionData(m_leftXTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 112;
	inputError = vr::VRInput()->GetDigitalActionData(m_leftXClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 113;
	if (digitalClickAction.bState)
	{
		output->X = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->X = 0.5f;
	}
	else
	{
		output->X = 0.0f;
	}

	inputError = vr::VRInput()->GetDigitalActionData(m_leftYTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 114;
	inputError = vr::VRInput()->GetDigitalActionData(m_leftYClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 115;
	if (digitalClickAction.bState)
	{
		output->Y = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->Y = 0.5f;
	}
	else
	{
		output->Y = 0.0f;
	}

	inputError = vr::VRInput()->GetDigitalActionData(m_rightATouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 116;
	inputError = vr::VRInput()->GetDigitalActionData(m_rightAClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 117;
	if (digitalClickAction.bState)
	{
		output->A = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->A = 0.5f;
	}
	else
	{
		output->A = 0.0f;
	}

	inputError = vr::VRInput()->GetDigitalActionData(m_rightBTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 118;
	inputError = vr::VRInput()->GetDigitalActionData(m_rightBClick, &digitalClickAction, sizeof(digitalClickAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 119;
	if (digitalClickAction.bState)
	{
		output->B = 1.0f;
	}
	else if (digitalTouchAction.bState)
	{
		output->B = 0.5f;
	}
	else
	{
		output->B = 0.0f;
	}

	inputError = vr::VRInput()->GetDigitalActionData(m_leftThumbRestTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 120;
	if (digitalTouchAction.bState)
	{
		output->LeftThumbRest = 0.5f;
	}
	else
	{
		output->LeftThumbRest = 0.0f;
	}


	inputError = vr::VRInput()->GetDigitalActionData(m_rightThumbRestTouch, &digitalTouchAction, sizeof(digitalTouchAction), vr::k_ulInvalidInputValueHandle);
	if (inputError != vr::VRInputError_None)
		return 121;
	if (digitalTouchAction.bState)
	{
		output->RightThumbRest = 0.5f;
	}
	else
	{
		output->RightThumbRest = 0.0f;
	}

	return 0;
}
int ovr_freepie_trigger_haptic_pulse(unsigned int controllerIndex, float duration, float frequency, float amplitude)
{
	if (controllerIndex == 0)
	{
		vr::EVRInputError inputError = vr::VRInput()->TriggerHapticVibrationAction(m_leftHaptic, 0, duration, frequency, amplitude, vr::k_ulInvalidInputValueHandle);
		if (inputError != vr::VRInputError_None)
			return 120;
	}
	else
	{
		vr::EVRInputError inputError = vr::VRInput()->TriggerHapticVibrationAction(m_rightHaptic, 0, duration, frequency, amplitude, vr::k_ulInvalidInputValueHandle);
		if (inputError != vr::VRInputError_None)
			return 121;
	}

	return 0;
}

int ovr_freepie_destroy()
{
	vr::VR_Shutdown();

	return 0;
}