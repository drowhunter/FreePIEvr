extern "C"
{
  #include "../include/ovr_freepie.h"
}

#include <iostream>
#include <fstream>
#include <ctime>
#include <Windows.h>
#include <string.h>
#include <vector>
#include <limits.h>


typedef struct ovr_freepie_haptics
{
	float Duration;
	float Frequency;
	float Amplitude;

} ovr_freepie_haptics;

typedef struct ovr_freepie_input
{
	ovr_freepie_haptics LeftHaptics;
	ovr_freepie_haptics RightHaptics;

	unsigned int BlockedInputs;
} ovr_freepie_input;


// memory mapped file
HANDLE hOutputFileMapping;
HANDLE hInputFileMapping;
LPVOID lpOutputMappedAddress; 
LPVOID lpInputMappedAddress;

// input/output
ovr_freepie_data* m_output;
ovr_freepie_input* m_input;

int InitIPC()
{
	// Create or open the memory-mapped file for output
	hOutputFileMapping = CreateFileMapping(
		INVALID_HANDLE_VALUE,           // Use paging file
		NULL,                          // Default security
		PAGE_READWRITE,                // Read/write access
		0,                             // Maximum object size (high-order DWORD)
		sizeof(m_output),  // Maximum object size (low-order DWORD)
		"FreePIEOpenXROutput");        // Name of mapping object
	if (hOutputFileMapping == NULL)
		return 1;

	// Create or open the memory-mapped file for haptics
	hInputFileMapping = CreateFileMapping(
		INVALID_HANDLE_VALUE,           // Use paging file
		NULL,                          // Default security
		PAGE_READWRITE,                // Read/write access
		0,                             // Maximum object size (high-order DWORD)
		sizeof(m_output),  // Maximum object size (low-order DWORD)
		"FreePIEOpenXRHaptics");        // Name of mapping object
	if (hInputFileMapping == NULL)
		return 2;

	// Map the output file to memory
	lpOutputMappedAddress = MapViewOfFile(
		hOutputFileMapping,                  // Handle to file mapping object
		FILE_MAP_ALL_ACCESS,           // Read/write access
		0,                             // High-order DWORD of offset
		0,                             // Low-order DWORD of offset
		0);                            // Number of bytes to map; 0 means map the whole file
	if (lpOutputMappedAddress == NULL) {
		CloseHandle(hOutputFileMapping);
		return 3;
	}

	// Map the haptics file to memory
	lpInputMappedAddress = MapViewOfFile(
		hInputFileMapping,                  // Handle to file mapping object
		FILE_MAP_ALL_ACCESS,           // Read/write access
		0,                             // High-order DWORD of offset
		0,                             // Low-order DWORD of offset
		0);                            // Number of bytes to map; 0 means map the whole file
	if (lpInputMappedAddress == NULL) {
		CloseHandle(hInputFileMapping);
		return 4;
	}

	m_output = reinterpret_cast<ovr_freepie_data*>(lpOutputMappedAddress);
	m_input = reinterpret_cast<ovr_freepie_input*>(lpInputMappedAddress);
	
	return 0;
}

int ovr_freepie_init()
{
	int result = InitIPC();
	if (result != 0)
		return result;
	
	ovr_freepie_configure_input(0);
	m_output->HmdMounted = false;

	return 0;
}

int ovr_freepie_destroy()
{
	if (lpOutputMappedAddress != NULL)
	{
		UnmapViewOfFile(lpOutputMappedAddress);
		lpOutputMappedAddress = NULL;
	}
	if (hOutputFileMapping != NULL)
	{
		CloseHandle(hOutputFileMapping);
		hOutputFileMapping = NULL;
	}

	if (lpInputMappedAddress != NULL)
	{
		UnmapViewOfFile(lpInputMappedAddress);
		lpInputMappedAddress = NULL;
	}
	if (hInputFileMapping != NULL)
	{
		CloseHandle(hInputFileMapping);
		hInputFileMapping = NULL;
	}

	return 0;
}

int ovr_freepie_reset_orientation()
{
	return 0;
}

int ovr_freepie_read(ovr_freepie_data *output)
{
	std::memcpy(output, m_output, sizeof(ovr_freepie_data));

	return 0;
}

int ovr_freepie_configure_input(unsigned int inputConfig)
{
	if (inputConfig)
	{
		m_input->BlockedInputs = inputConfig;
	}

	return 0;
}

int ovr_freepie_trigger_haptic_pulse(unsigned int controllerIndex, float duration, float frequency, float amplitude)
{
	if (controllerIndex == 0)
	{
		m_input->LeftHaptics.Amplitude = amplitude;
		m_input->LeftHaptics.Duration = duration;
		m_input->LeftHaptics.Frequency = frequency;
	}
	else
	{
		m_input->RightHaptics.Amplitude = amplitude;
		m_input->RightHaptics.Duration = duration;
		m_input->RightHaptics.Frequency = frequency;
	}
	return 0;
}


namespace openxr_api_layer 
{
	const char* _layerName = NULL;

	// methods from next layer
	PFN_xrStringToPath _nextXrStringToPath = NULL;
	PFN_xrPathToString _nextXrPathToString = NULL;
	PFN_xrGetInstanceProcAddr _nextXrGetInstanceProcAddr = NULL;
	PFN_xrCreateSession _nextXrCreateSession = NULL;
	PFN_xrCreateActionSet _nextXrCreateActionSet = NULL;
	PFN_xrCreateAction _nextXrCreateAction = NULL;
	PFN_xrCreateActionSpace _nextXrCreateActionSpace = NULL;
	PFN_xrCreateReferenceSpace _nextXrCreateReferenceSpace = NULL;
	PFN_xrSuggestInteractionProfileBindings _nextXrSuggestInteractionProfileBindings = NULL;
	PFN_xrAttachSessionActionSets _nextXrAttachSessionActionSets = NULL;
	PFN_xrWaitFrame _nextXrWaitFrame = NULL;
	PFN_xrBeginFrame _nextXrBeginFrame = NULL;
	PFN_xrSyncActions _nextXrSyncActions = NULL;
	PFN_xrGetActionStatePose _nextXrGetActionStatePose = NULL;
	PFN_xrLocateSpace _nextXrLocateSpace = NULL;
	PFN_xrGetActionStateFloat _nextXrGetActionStateFloat = NULL;
	PFN_xrGetActionStateBoolean _nextXrGetActionStateBoolean = NULL;
	PFN_xrApplyHapticFeedback _nextXrApplyHapticFeedback = NULL;

	// cache instance/session info
	XrInstanceCreateInfo m_instanceInfo;
	XrInstance m_instance;
	XrSystemId m_system;
	XrSession m_session;
	XrTime m_predictedTime;
	XrDuration m_predictedDuration;

	// actions
	XrActionSet m_actionSet;
	XrAction m_handPoseAction;
	XrAction m_triggerAction;
	XrAction m_gripAction;
	XrAction m_stickXAction;
	XrAction m_stickYAction;
	XrAction m_stickClickAction;
	XrAction m_stickTouchAction;
	XrAction m_aClickAction;
	XrAction m_aTouchAction;
	XrAction m_bClickAction;
	XrAction m_bTouchAction;
	XrAction m_hapticAction;

	XrPath m_handPaths[2] = { 0, 0 };

	XrActionStatePose m_handPoseState[2] = { {XR_TYPE_ACTION_STATE_POSE}, {XR_TYPE_ACTION_STATE_POSE} };
	XrActionStateFloat m_floatState[2] = { {XR_TYPE_ACTION_STATE_FLOAT}, {XR_TYPE_ACTION_STATE_FLOAT} };
	XrActionStateBoolean m_clickState[2] = { {XR_TYPE_ACTION_STATE_BOOLEAN}, {XR_TYPE_ACTION_STATE_BOOLEAN} };
	XrActionStateBoolean m_touchState[2] = { {XR_TYPE_ACTION_STATE_BOOLEAN}, {XR_TYPE_ACTION_STATE_BOOLEAN} };

	XrSpace m_localSpace = XR_NULL_HANDLE;
	XrSpace m_headPoseSpace;
	XrSpace m_handPoseSpace[2];
	XrPosef m_handPose[2] = {
		{{1.0f, 0.0f, 0.0f, 0.0f}, {0.0f, 0.0f, 0.0f}},
		{{1.0f, 0.0f, 0.0f, 0.0f}, {0.0f, 0.0f, 0.0f}} };

	// bindings
	std::vector<XrActionSuggestedBinding> m_bindings_simple_controller;
	std::vector<XrActionSuggestedBinding> m_bindings_vive_controller;
	std::vector<XrActionSuggestedBinding> m_bindings_motion_controller;
	std::vector<XrActionSuggestedBinding> m_bindings_touch_controller;
	std::vector<XrActionSuggestedBinding> m_bindings_index_controller;


	XrPath CreateXrPath(const char* path_string) {
		XrPath xrPath;
		XrResult result = _nextXrStringToPath(m_instance, path_string, &xrPath);
		if (XR_SUCCEEDED(result))
			return xrPath;

		return XR_NULL_PATH;
	}
	std::string FromXrPath(XrPath path) {
		uint32_t strl;
		char text[XR_MAX_PATH_LENGTH];
		XrResult result = _nextXrPathToString(m_instance, path, XR_MAX_PATH_LENGTH, &strl, text);
		if (XR_SUCCEEDED(result))
			return text;

		return nullptr;
	}

	bool CreateAction(XrAction& xrAction, const char* name, XrActionType xrActionType, std::vector<const char*> subaction_paths = {})
	{
		XrActionCreateInfo actionCI{ XR_TYPE_ACTION_CREATE_INFO };
		// The type of action: float input, pose, haptic output etc.
		actionCI.actionType = xrActionType;
		// Subaction paths, e.g. left and right hand. To distinguish the same action performed on different devices.
		std::vector<XrPath> subaction_xrpaths;
		for (auto p : subaction_paths) {
			subaction_xrpaths.push_back(CreateXrPath(p));
		}
		actionCI.countSubactionPaths = (uint32_t)subaction_xrpaths.size();
		actionCI.subactionPaths = subaction_xrpaths.data();
		// The internal name the runtime uses for this Action.
		strncpy_s(actionCI.actionName, name, XR_MAX_ACTION_NAME_SIZE);
		// Localized names are required so there is a human-readable action name to show the user if they are rebinding the Action in an options screen.
		strncpy_s(actionCI.localizedActionName, name, XR_MAX_LOCALIZED_ACTION_NAME_SIZE);
		XrResult result = _nextXrCreateAction(m_actionSet, &actionCI, &xrAction);

		return XR_SUCCEEDED(result);
	};

	void log(const std::string& message)
	{
		// Open log file
		std::ofstream logfile("x:\freepie_opexr_log.txt", std::ios::app); // Open for appending

		// Write log message with timestamp to file
		if (logfile.is_open()) {
			logfile << _layerName << ": " << message << std::endl;
			logfile.close();
		}
	}

	XrResult SuggestBindings(const char* profile_path, std::vector<XrActionSuggestedBinding> bindings)
	{
		// The application can call xrSuggestInteractionProfileBindings once per interaction profile that it supports.
		XrInteractionProfileSuggestedBinding interactionProfileSuggestedBinding{ XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING };
		interactionProfileSuggestedBinding.interactionProfile = CreateXrPath(profile_path);
		interactionProfileSuggestedBinding.suggestedBindings = bindings.data();
		interactionProfileSuggestedBinding.countSuggestedBindings = (uint32_t)bindings.size();
		return _nextXrSuggestInteractionProfileBindings(m_instance, &interactionProfileSuggestedBinding);
	};

	XrSpace CreateActionPoseSpace(XrAction xrAction, const char* subaction_path = nullptr)
	{
		XrSpace xrSpace;
		const XrPosef xrPoseIdentity = { {0.0f, 0.0f, 0.0f, 1.0f}, {0.0f, 0.0f, 0.0f} };
		// Create frame of reference for a pose action
		XrActionSpaceCreateInfo actionSpaceCI{ XR_TYPE_ACTION_SPACE_CREATE_INFO };
		actionSpaceCI.action = xrAction;
		actionSpaceCI.poseInActionSpace = xrPoseIdentity;
		if (subaction_path)
			actionSpaceCI.subactionPath = CreateXrPath(subaction_path);
		XrResult result = _nextXrCreateActionSpace(m_session, &actionSpaceCI, &xrSpace);
		if (XR_SUCCEEDED(result))
			return xrSpace;

		return 0;
	};

	int ovr_freepie_setPose(XrPosef* pose, ovr_freepie_6dof* dof)
	{
		// from https://gitlab.freedesktop.org/monado/monado/-/blob/main/src/xrt/auxiliary/math/m_base.cpp?ref_type=heads
		//struct xrt_matrix_3x3 result = { {
		//	1 - 2 * q->y * q->y - 2 * q->z * q->z,
		//	2 * q->x * q->y - 2 * q->w * q->z,
		//	2 * q->x * q->z + 2 * q->w * q->y,
		//
		//	2 * q->x * q->y + 2 * q->w * q->z,
		//	1 - 2 * q->x * q->x - 2 * q->z * q->z,
		//	2 * q->y * q->z - 2 * q->w * q->x,
		//
		//	2 * q->x * q->z - 2 * q->w * q->y,
		//	2 * q->y * q->z + 2 * q->w * q->x,
		//	1 - 2 * q->x * q->x - 2 * q->y * q->y,
		//} };

		//dof->left[0] = matrix.m[0][0];
		//dof->left[1] = matrix.m[1][0];
		//dof->left[2] = matrix.m[2][0];
		//
		//dof->up[0] = matrix.m[0][1];
		//dof->up[1] = matrix.m[1][1];
		//dof->up[2] = matrix.m[2][1];
		//
		//dof->forward[0] = matrix.m[0][2];
		//dof->forward[1] = matrix.m[1][2];
		//dof->forward[2] = matrix.m[2][2];

		float x = pose->orientation.x;
		float y = pose->orientation.y;
		float z = pose->orientation.z;
		float w = pose->orientation.w;
		float s = x * x + y * y + z * z + w * w;
		s = 1 / s;

		dof->left[0] = -1 + 2 * s * (y * y + z * z);
		dof->left[1] = 2 * s * (x * y + z * w);
		dof->left[2] = 2 * s * (x * z - y * w);

		dof->up[0] = -2 * s * (x * y - z * w);
		dof->up[1] = 1 - 2 * s * (x * x + z * z);
		dof->up[2] = 2 * s * (y * z + x * w);

		dof->forward[0] = -2 * s * (x * z - y * w);
		dof->forward[1] = 2 * s * (y * z - x * w);
		dof->forward[2] = 1 - 2 * s * (x * x + y * y);

		dof->position[0] = pose->position.x;
		dof->position[1] = pose->position.y;
		dof->position[2] = pose->position.z;
		return  0;
	}

	XrResult _xrCreateSession(XrInstance instance, const XrSessionCreateInfo* createInfo, XrSession* session)
	{
		// first let session be created
		XrResult result = _nextXrCreateSession(instance, createInfo, session);
		m_session = *session;
		if (XR_FAILED(result))
			return result;

		// create action set
		XrActionSetCreateInfo actionSetCI{ XR_TYPE_ACTION_SET_CREATE_INFO };
		strncpy_s(actionSetCI.actionSetName, "freepie-actionset", XR_MAX_ACTION_SET_NAME_SIZE);
		strncpy_s(actionSetCI.localizedActionSetName, "FreePIE", XR_MAX_LOCALIZED_ACTION_SET_NAME_SIZE);
		result = _nextXrCreateActionSet(m_instance, &actionSetCI, &m_actionSet);
		if (XR_FAILED(result))
			return result;
		actionSetCI.priority = 0;

		// Create actions
		CreateAction(m_handPoseAction, "head-pose", XR_ACTION_TYPE_POSE_INPUT, { "/user/head" });
		CreateAction(m_handPoseAction, "hand-pose", XR_ACTION_TYPE_POSE_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_triggerAction, "trigger", XR_ACTION_TYPE_FLOAT_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_gripAction, "grip", XR_ACTION_TYPE_FLOAT_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_stickXAction, "stick-x", XR_ACTION_TYPE_FLOAT_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_stickYAction, "stick-y", XR_ACTION_TYPE_FLOAT_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_stickClickAction, "stick-click", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_stickTouchAction, "stick-touch", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_aClickAction, "a-click", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_aTouchAction, "a-touch", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_bClickAction, "b-click", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_bTouchAction, "b-touch", XR_ACTION_TYPE_BOOLEAN_INPUT, { "/user/hand/left", "/user/hand/right" });
		CreateAction(m_hapticAction, "haptic", XR_ACTION_TYPE_VIBRATION_OUTPUT, { "/user/hand/left", "/user/hand/right" });


		// Fill out an XrReferenceSpaceCreateInfo structure and create a reference XrSpace, specifying a Local space with an identity pose as the origin.
		XrReferenceSpaceCreateInfo referenceSpaceCI{ XR_TYPE_REFERENCE_SPACE_CREATE_INFO };
		referenceSpaceCI.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_LOCAL;
		referenceSpaceCI.poseInReferenceSpace = { {0.0f, 0.0f, 0.0f, 1.0f}, {0.0f, 0.0f, 0.0f} };
		result = _nextXrCreateReferenceSpace(m_session, &referenceSpaceCI, &m_localSpace);
		if (XR_FAILED(result))
			return result;

		m_handPoseSpace[0] = CreateActionPoseSpace(m_handPoseAction, "/user/hand/left");
		m_handPoseSpace[1] = CreateActionPoseSpace(m_handPoseAction, "/user/hand/right");

		XrReferenceSpaceCreateInfo headPoseSpaceInfo = {
			XR_TYPE_REFERENCE_SPACE_CREATE_INFO,
			NULL,
			XR_REFERENCE_SPACE_TYPE_VIEW,
			{ {0, 0, 0, 1}, {0, 0, 0} }
		};
		result = _nextXrCreateReferenceSpace(m_session, &headPoseSpaceInfo, &m_headPoseSpace);
		if (XR_FAILED(result))
			return result;

		// For later convenience we create the XrPaths for the subaction path names.
		m_handPaths[0] = CreateXrPath("/user/hand/left");
		m_handPaths[1] = CreateXrPath("/user/hand/right");

		return result;
	}

	bool IsInputBlocked(int bit)
	{
		return m_input->BlockedInputs & (1 << bit);
	}

	bool AllowSuggestion(XrPath path)
	{
		// always allow poses and haptics for left controller
		std::string pathString = FromXrPath(path);
		//std::ofstream file;
		//file.open("x:/test.txt", std::ios::out | std::ios::app);
		//file << "suggestion " << pathString << std::endl;
		if (pathString == "/user/hand/left/input/aim/pose")
			return true;
		if (pathString == "/user/hand/left/input/grip/pose")
			return true;
		if (pathString == "/user/hand/left/output/haptic")
			return true;

		// always allow poses and haptics for right controller
		if (pathString == "/user/hand/right/input/aim/pose")
			return true;
		if (pathString == "/user/hand/right/input/grip/pose")
			return true;
		if (pathString == "/user/hand/right/output/haptic")
			return true;

		// if all bits are set, no further checks required
		if (m_input->BlockedInputs == UINT_MAX)
			return false;

		int i = 0;

		// left grip
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/select")
				return false;
			if (pathString == "/user/hand/left/input/select/click")
				return false;
			if (pathString == "/user/hand/left/input/squeeze")
				return false;
			if (pathString == "/user/hand/left/input/squeeze/click")
				return false;
			if (pathString == "/user/hand/left/input/squeeze/value")
				return false;
		}
		// left trigger
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/trigger")
				return false;
			if (pathString == "/user/hand/left/input/trigger/value")
				return false;
		}
		// right grip
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/select")
				return false;
			if (pathString == "/user/hand/right/input/select/click")
				return false;
			if (pathString == "/user/hand/right/input/squeeze")
				return false;
			if (pathString == "/user/hand/right/input/squeeze/click")
				return false;
			if (pathString == "/user/hand/right/input/squeeze/value")
				return false;
		}
		// right trigger
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/trigger")
				return false;
			if (pathString == "/user/hand/right/input/trigger/value")
				return false;
		}

		// right a
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/a")
				return false;
			if (pathString == "/user/hand/right/input/a/click")
				return false;
			if (pathString == "/user/hand/right/input/a/touch")
				return false;
		}
		// right b
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/b")
				return false;
			if (pathString == "/user/hand/right/input/b/click")
				return false;
			if (pathString == "/user/hand/right/input/b/touch")
				return false;
		}

		// left x (a)
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/x")
				return false;
			if (pathString == "/user/hand/left/input/x/click")
				return false;
			if (pathString == "/user/hand/left/input/x/touch")
				return false;
			if (pathString == "/user/hand/left/input/a")
				return false;
			if (pathString == "/user/hand/left/input/a/click")
				return false;
			if (pathString == "/user/hand/left/input/a/touch")
				return false;
		}
		// left y (b)
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/y")
				return false;
			if (pathString == "/user/hand/left/input/y/click")
				return false;
			if (pathString == "/user/hand/left/input/y/touch")
				return false;
			if (pathString == "/user/hand/left/input/b")
				return false;
			if (pathString == "/user/hand/left/input/b/click")
				return false;
			if (pathString == "/user/hand/left/input/b/touch")
				return false;
		}

		// left stick
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/trackpad")
				return false;
			if (pathString == "/user/hand/left/input/trackpad/x")
				return false;
			if (pathString == "/user/hand/left/input/trackpad/y")
				return false;
			if (pathString == "/user/hand/left/input/thumbstick")
				return false;
			if (pathString == "/user/hand/left/input/thumbstick/x")
				return false;
			if (pathString == "/user/hand/left/input/thumbstick/y")
				return false;
		}
		// left stick click
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/left/input/trackpad/click")
				return false;
			if (pathString == "/user/hand/left/input/trackpad/touch")
				return false;
			if (pathString == "/user/hand/left/input/thumbstick/click")
				return false;
			if (pathString == "/user/hand/left/input/thumbstick/touch")
				return false;
		}

		// right stick
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/trackpad")
				return false;
			if (pathString == "/user/hand/right/input/trackpad/x")
				return false;
			if (pathString == "/user/hand/right/input/trackpad/y")
				return false;
			if (pathString == "/user/hand/right/input/thumbstick")
				return false;
			if (pathString == "/user/hand/right/input/thumbstick/x")
				return false;
			if (pathString == "/user/hand/right/input/thumbstick/y")
				return false;
		}
		// right stick click
		if (IsInputBlocked(i++))
		{
			if (pathString == "/user/hand/right/input/trackpad/click")
				return false;
			if (pathString == "/user/hand/right/input/trackpad/touch")
				return false;
			if (pathString == "/user/hand/right/input/thumbstick/click")
				return false;
			if (pathString == "/user/hand/right/input/thumbstick/touch")
				return false;
		}

		return true;
	}

	XrResult _xrSuggestInteractionProfileBindings(XrInstance instance, const XrInteractionProfileSuggestedBinding* suggestedBindings)
	{
		XrResult result = _nextXrSuggestInteractionProfileBindings(instance, suggestedBindings);
		if (XR_FAILED(result))
			return result;

		std::string profile = FromXrPath(suggestedBindings->interactionProfile);
		
		// join bindings
		if (profile == "/interaction_profiles/khr/simple_controller")
		{
			for (unsigned int i = 0; i < suggestedBindings->countSuggestedBindings; ++i)
			{
				if (AllowSuggestion(suggestedBindings->suggestedBindings[i].binding))
				{
					m_bindings_simple_controller.push_back(suggestedBindings->suggestedBindings[i]);
				}
			}
		}
		else if (profile == "/interaction_profiles/htc/vive_controller")
		{
			for (unsigned int i = 0; i < suggestedBindings->countSuggestedBindings; ++i)
			{
				if (AllowSuggestion(suggestedBindings->suggestedBindings[i].binding))
				{
					m_bindings_vive_controller.push_back(suggestedBindings->suggestedBindings[i]);					
				}
			}
		}
		else if (profile == "/interaction_profiles/microsoft/motion_controller")
		{
			for (unsigned int i = 0; i < suggestedBindings->countSuggestedBindings; ++i)
			{
				if (AllowSuggestion(suggestedBindings->suggestedBindings[i].binding))
				{
					m_bindings_motion_controller.push_back(suggestedBindings->suggestedBindings[i]);
				}
			}
		}
		else if (profile == "/interaction_profiles/oculus/touch_controller")
		{
			for (unsigned int i = 0; i < suggestedBindings->countSuggestedBindings; ++i)
			{
				if (AllowSuggestion(suggestedBindings->suggestedBindings[i].binding))
				{
					m_bindings_touch_controller.push_back(suggestedBindings->suggestedBindings[i]);
				}
			}
		}
		else if (profile == "/interaction_profiles/valve/index_controller")
		{
			for (unsigned int i = 0; i < suggestedBindings->countSuggestedBindings; ++i)
			{
				if (AllowSuggestion(suggestedBindings->suggestedBindings[i].binding))
				{
					m_bindings_index_controller.push_back(suggestedBindings->suggestedBindings[i]);
				}
			}
		}

		return XR_SUCCESS;
	}

	XrResult _xrAttachSessionActionSets(XrSession session, const XrSessionActionSetsAttachInfo* attachInfo)
	{
		XrResult result;
		// provide bindings
		// general
		{
			m_bindings_simple_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/left/input/select/click") });
			m_bindings_simple_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/right/input/select/click") });
			m_bindings_simple_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose") });
			m_bindings_simple_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose") });
			m_bindings_simple_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/left/output/haptic") });
			m_bindings_simple_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/right/output/haptic") });

			result = SuggestBindings("/interaction_profiles/khr/simple_controller", m_bindings_simple_controller);
			if (XR_FAILED(result))
				return result;
		}

		// htc vive
		{
			m_bindings_vive_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/click") });
			m_bindings_vive_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/click") });
			m_bindings_vive_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/left/input/trackpad/x") });
			m_bindings_vive_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/right/input/trackpad/x") });
			m_bindings_vive_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/left/input/trackpad/y") });
			m_bindings_vive_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/right/input/trackpad/y") });
			m_bindings_vive_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/left/input/trackpad/click") });
			m_bindings_vive_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/right/input/trackpad/click") });
			m_bindings_vive_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/left/input/trackpad/touch") });
			m_bindings_vive_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/right/input/trackpad/touch") });
			m_bindings_vive_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value") });
			m_bindings_vive_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value") });
			m_bindings_vive_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose") });
			m_bindings_vive_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose") });
			m_bindings_vive_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/left/output/haptic") });
			m_bindings_vive_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/right/output/haptic") });

			result = SuggestBindings("/interaction_profiles/htc/vive_controller", m_bindings_vive_controller);
			if (XR_FAILED(result))
				return result;
		}

		// mxr		
		{
			m_bindings_motion_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/click") });
			m_bindings_motion_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/click") });
			m_bindings_motion_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/left/input/trackpad/x") });
			m_bindings_motion_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/right/input/trackpad/x") });
			m_bindings_motion_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/left/input/trackpad/y") });
			m_bindings_motion_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/right/input/trackpad/y") });
			m_bindings_motion_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/left/input/trackpad/click") });
			m_bindings_motion_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/right/input/trackpad/click") });
			m_bindings_motion_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/left/input/trackpad/touch") });
			m_bindings_motion_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/right/input/trackpad/touch") });
			m_bindings_motion_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value") });
			m_bindings_motion_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value") });
			m_bindings_motion_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose") });
			m_bindings_motion_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose") });
			m_bindings_motion_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/left/output/haptic") });
			m_bindings_motion_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/right/output/haptic") });

			result = SuggestBindings("/interaction_profiles/microsoft/motion_controller", m_bindings_motion_controller);
			if (XR_FAILED(result))
				return result;
		}

		// oculus
		{
			m_bindings_touch_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/value") });
			m_bindings_touch_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/value") });
			m_bindings_touch_controller.push_back({ m_aClickAction, CreateXrPath("/user/hand/left/input/x/click") });
			m_bindings_touch_controller.push_back({ m_aClickAction, CreateXrPath("/user/hand/right/input/a/click") });
			m_bindings_touch_controller.push_back({ m_aTouchAction, CreateXrPath("/user/hand/left/input/x/touch") });
			m_bindings_touch_controller.push_back({ m_aTouchAction, CreateXrPath("/user/hand/right/input/a/touch") });
			m_bindings_touch_controller.push_back({ m_bClickAction, CreateXrPath("/user/hand/left/input/y/click") });
			m_bindings_touch_controller.push_back({ m_bClickAction, CreateXrPath("/user/hand/right/input/b/click") });
			m_bindings_touch_controller.push_back({ m_bTouchAction, CreateXrPath("/user/hand/left/input/y/touch") });
			m_bindings_touch_controller.push_back({ m_bTouchAction, CreateXrPath("/user/hand/right/input/b/touch") });
			m_bindings_touch_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/left/input/thumbstick/x") });
			m_bindings_touch_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/right/input/thumbstick/x") });
			m_bindings_touch_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/left/input/thumbstick/y") });
			m_bindings_touch_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/right/input/thumbstick/y") });
			m_bindings_touch_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/left/input/thumbstick/click") });
			m_bindings_touch_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/right/input/thumbstick/click") });
			m_bindings_touch_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/left/input/thumbstick/touch") });
			m_bindings_touch_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/right/input/thumbstick/touch") });
			m_bindings_touch_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value") });
			m_bindings_touch_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value") });
			m_bindings_touch_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose") });
			m_bindings_touch_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose") });
			m_bindings_touch_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/left/output/haptic") });
			m_bindings_touch_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/right/output/haptic") });

			result = SuggestBindings("/interaction_profiles/oculus/touch_controller", m_bindings_touch_controller);
			if (XR_FAILED(result))
				return result;
		}

		// valve index
		{
			m_bindings_index_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/value") });
			m_bindings_index_controller.push_back({ m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/value") });
			m_bindings_index_controller.push_back({ m_aClickAction, CreateXrPath("/user/hand/left/input/a/click") });
			m_bindings_index_controller.push_back({ m_aClickAction, CreateXrPath("/user/hand/right/input/a/click") });
			m_bindings_index_controller.push_back({ m_aTouchAction, CreateXrPath("/user/hand/left/input/a/touch") });
			m_bindings_index_controller.push_back({ m_aTouchAction, CreateXrPath("/user/hand/right/input/a/touch") });
			m_bindings_index_controller.push_back({ m_bClickAction, CreateXrPath("/user/hand/left/input/b/click") });
			m_bindings_index_controller.push_back({ m_bClickAction, CreateXrPath("/user/hand/right/input/b/click") });
			m_bindings_index_controller.push_back({ m_bTouchAction, CreateXrPath("/user/hand/left/input/b/touch") });
			m_bindings_index_controller.push_back({ m_bTouchAction, CreateXrPath("/user/hand/right/input/b/touch") });
			m_bindings_index_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/left/input/thumbstick/x") });
			m_bindings_index_controller.push_back({ m_stickXAction, CreateXrPath("/user/hand/right/input/thumbstick/x") });
			m_bindings_index_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/left/input/thumbstick/y") });
			m_bindings_index_controller.push_back({ m_stickYAction, CreateXrPath("/user/hand/right/input/thumbstick/y") });
			m_bindings_index_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/left/input/thumbstick/click") });
			m_bindings_index_controller.push_back({ m_stickClickAction, CreateXrPath("/user/hand/right/input/thumbstick/click") });
			m_bindings_index_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/left/input/thumbstick/touch") });
			m_bindings_index_controller.push_back({ m_stickTouchAction, CreateXrPath("/user/hand/right/input/thumbstick/touch") });
			m_bindings_index_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value") });
			m_bindings_index_controller.push_back({ m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value") });
			m_bindings_index_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose") });
			m_bindings_index_controller.push_back({ m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose") });
			m_bindings_index_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/left/output/haptic") });
			m_bindings_index_controller.push_back({ m_hapticAction, CreateXrPath("/user/hand/right/output/haptic") });

			result = SuggestBindings("/interaction_profiles/valve/index_controller", m_bindings_index_controller);
			if (XR_FAILED(result))
				return result;
		}

		// join action sets
		std::vector<XrActionSet> actionSets;
		for (unsigned int i = 0; i < attachInfo->countActionSets; ++i)
		{
			actionSets.push_back(attachInfo->actionSets[i]);
		}
		actionSets.push_back(m_actionSet);

		XrSessionActionSetsAttachInfo actionSetAttachInfo{ XR_TYPE_SESSION_ACTION_SETS_ATTACH_INFO };
		actionSetAttachInfo.countActionSets = attachInfo->countActionSets + 1;
		actionSetAttachInfo.actionSets = actionSets.data();
		return _nextXrAttachSessionActionSets(m_session, &actionSetAttachInfo);
	}

	XrResult _xrWaitFrame(XrSession session, const XrFrameWaitInfo* frameWaitInfo, XrFrameState* frameState)
	{
		// first communicate
		XrResult result = _nextXrWaitFrame(session, frameWaitInfo, frameState);
		if (XR_FAILED(result))
			return result;

		m_predictedTime = frameState->predictedDisplayTime;
		m_predictedDuration = frameState->predictedDisplayPeriod;
		return result;
	}

	XrResult UpdateOutput()
	{
		// Update our action set with up-to-date input data.
		// First, we specify the actionSet we are polling.
		XrActiveActionSet activeActionSet{};
		activeActionSet.actionSet = m_actionSet;
		activeActionSet.subactionPath = XR_NULL_PATH;
		// Now we sync the Actions to make sure they have current data.
		XrActionsSyncInfo actionsSyncInfo{ XR_TYPE_ACTIONS_SYNC_INFO };
		actionsSyncInfo.countActiveActionSets = 1;
		actionsSyncInfo.activeActionSets = &activeActionSet;
		XrResult result = _nextXrSyncActions(m_session, &actionsSyncInfo);
		if (XR_FAILED(result))
			return result;

		// get actions
		XrActionStateGetInfo actionStateGetInfo{ XR_TYPE_ACTION_STATE_GET_INFO };

		// hand poses
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_handPoseAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStatePose(m_session, &actionStateGetInfo, &m_handPoseState[i]);
			if (XR_FAILED(result))
				return result;

			if (m_handPoseState[i].isActive) {
				XrSpaceLocation spaceLocation{ XR_TYPE_SPACE_LOCATION };
				XrResult res = _nextXrLocateSpace(m_handPoseSpace[i], m_localSpace, m_predictedTime, &spaceLocation);
				if (XR_UNQUALIFIED_SUCCESS(res) &&
					(spaceLocation.locationFlags & XR_SPACE_LOCATION_POSITION_VALID_BIT) != 0 &&
					(spaceLocation.locationFlags & XR_SPACE_LOCATION_ORIENTATION_VALID_BIT) != 0) {
					m_handPose[i] = spaceLocation.pose;
				}
				else {
					m_handPoseState[i].isActive = false;
				}
			}
		}
		m_output->StatusLeftHand = m_handPoseState[0].isActive ? 1 : 0;
		m_output->StatusRightHand = m_handPoseState[0].isActive ? 1 : 0;
		ovr_freepie_setPose(&m_handPose[0], &m_output->leftHand);
		ovr_freepie_setPose(&m_handPose[1], &m_output->rightHand);

		// trigger
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_triggerAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->LeftTrigger = m_floatState[0].currentState;
		m_output->RightTrigger = m_floatState[1].currentState;

		// grip
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_gripAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->LeftGrip = m_floatState[0].currentState;
		m_output->RightGrip = m_floatState[1].currentState;

		// sticks
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickXAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->LeftStickAxes[0] = m_floatState[0].currentState;
		m_output->RightStickAxes[0] = m_floatState[1].currentState;

		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickYAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->LeftStickAxes[1] = m_floatState[0].currentState;
		m_output->RightStickAxes[1] = m_floatState[1].currentState;

		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_FAILED(result))
				return result;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->LeftStick = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		m_output->RightStick = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);

		// a button
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_aClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_FAILED(result))
				return result;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_aTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->X = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		m_output->A = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);

		// b button
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_bClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_FAILED(result))
				return result;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_bTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = _nextXrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_FAILED(result))
				return result;
		}
		m_output->Y = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		m_output->B = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);

		// head position
		{
			XrSpaceLocation spaceLocation = { XR_TYPE_SPACE_LOCATION, NULL, 0, {{0, 0, 0, 1}, {0, 0, 0}} };
			result = _nextXrLocateSpace(m_headPoseSpace, m_localSpace, m_predictedTime, &spaceLocation);
			if (XR_FAILED(result))
				return result;

			if ((spaceLocation.locationFlags & XR_SPACE_LOCATION_POSITION_VALID_BIT) != 0 &&
				(spaceLocation.locationFlags & XR_SPACE_LOCATION_ORIENTATION_VALID_BIT) != 0) {

				ovr_freepie_setPose(&spaceLocation.pose, &m_output->head);
				m_output->StatusHead = 1;
			}
			else
			{
				m_output->StatusHead = 0;
			}
		}

		// vibrations
		XrHapticVibration vibration{ XR_TYPE_HAPTIC_VIBRATION };
		vibration.amplitude = m_input->LeftHaptics.Amplitude;
		vibration.duration = m_predictedDuration; // std::max(XR_MIN_HAPTIC_DURATION, (int)(1000 * 1000 * duration));
		vibration.frequency = XR_FREQUENCY_UNSPECIFIED;

		XrHapticActionInfo hapticActionInfo{ XR_TYPE_HAPTIC_ACTION_INFO };
		hapticActionInfo.action = m_hapticAction;
		hapticActionInfo.subactionPath = m_handPaths[0];
		result = _nextXrApplyHapticFeedback(m_session, &hapticActionInfo, (XrHapticBaseHeader*)&vibration);
		if (XR_FAILED(result))
			return result;

		vibration.amplitude = m_input->RightHaptics.Amplitude;
		hapticActionInfo.subactionPath = m_handPaths[1];
		result = _nextXrApplyHapticFeedback(m_session, &hapticActionInfo, (XrHapticBaseHeader*)&vibration);
		if (XR_FAILED(result))
			return result;

		return result;
	}

	XrResult _xrSyncActions(XrSession session, const XrActionsSyncInfo* syncInfo)
	{
		UpdateOutput();

		// finally sync actions communicate
		return _nextXrSyncActions(session, syncInfo);
	}

	XrResult xrGetInstanceProcAddr(XrInstance instance, const char* name, PFN_xrVoidFunction* function)
	{
		std::string func_name = name;
		if (func_name == "xrCreateSession")
		{
			*function = (PFN_xrVoidFunction)_xrCreateSession;
			return XR_SUCCESS;
		}

		if (func_name == "xrSuggestInteractionProfileBindings")
		{
			*function = (PFN_xrVoidFunction)_xrSuggestInteractionProfileBindings;
			return XR_SUCCESS;
		}

		if (func_name == "xrAttachSessionActionSets")
		{
			*function = (PFN_xrVoidFunction)_xrAttachSessionActionSets;
			return XR_SUCCESS;
		}

		if (func_name == "xrWaitFrame")
		{
			*function = (PFN_xrVoidFunction)_xrWaitFrame;
			return XR_SUCCESS;
		}

		if (func_name == "xrSyncActions")
		{
			*function = (PFN_xrVoidFunction)_xrSyncActions;
			return XR_SUCCESS;
		}

		return _nextXrGetInstanceProcAddr(instance, name, function);
	}

	// Entry point for creating the layer.
	XrResult xrCreateApiLayerInstance(const XrInstanceCreateInfo* const info, const struct XrApiLayerCreateInfo* const apiLayerInfo, XrInstance* const instance)
	{		
		// first let the instance be created
		XrResult result = apiLayerInfo->nextInfo->nextCreateApiLayerInstance(info, apiLayerInfo, instance);
		if (XR_FAILED(result))
			return result;

		// then use the created instance to load next function pointers
		_nextXrGetInstanceProcAddr = apiLayerInfo->nextInfo->nextGetInstanceProcAddr;
		result = _nextXrGetInstanceProcAddr(*instance, "xrCreateSession", (PFN_xrVoidFunction*)&_nextXrCreateSession);
		if (XR_FAILED(result))
			return result;
		
		result = _nextXrGetInstanceProcAddr(*instance, "xrStringToPath", (PFN_xrVoidFunction*)&_nextXrStringToPath);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrPathToString", (PFN_xrVoidFunction*)&_nextXrPathToString);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrCreateActionSet", (PFN_xrVoidFunction*)&_nextXrCreateActionSet);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrCreateAction", (PFN_xrVoidFunction*)&_nextXrCreateAction);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrCreateActionSpace", (PFN_xrVoidFunction*)&_nextXrCreateActionSpace);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrCreateReferenceSpace", (PFN_xrVoidFunction*)&_nextXrCreateReferenceSpace);
		if (XR_FAILED(result))
			return result;
		
		result = _nextXrGetInstanceProcAddr(*instance, "xrSuggestInteractionProfileBindings", (PFN_xrVoidFunction*)&_nextXrSuggestInteractionProfileBindings);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrAttachSessionActionSets", (PFN_xrVoidFunction*)&_nextXrAttachSessionActionSets);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrAttachSessionActionSets", (PFN_xrVoidFunction*)&_nextXrAttachSessionActionSets);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrWaitFrame", (PFN_xrVoidFunction*)&_nextXrWaitFrame);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrBeginFrame", (PFN_xrVoidFunction*)&_nextXrBeginFrame);
		if (XR_FAILED(result))
			return result;
		
		result = _nextXrGetInstanceProcAddr(*instance, "xrSyncActions", (PFN_xrVoidFunction*)&_nextXrSyncActions);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrGetActionStatePose", (PFN_xrVoidFunction*)&_nextXrGetActionStatePose);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrLocateSpace", (PFN_xrVoidFunction*)&_nextXrLocateSpace);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrGetActionStateFloat", (PFN_xrVoidFunction*)&_nextXrGetActionStateFloat);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrGetActionStateBoolean", (PFN_xrVoidFunction*)&_nextXrGetActionStateBoolean);
		if (XR_FAILED(result))
			return result;

		result = _nextXrGetInstanceProcAddr(*instance, "xrApplyHapticFeedback", (PFN_xrVoidFunction*)&_nextXrApplyHapticFeedback);
		if (XR_FAILED(result))
			return result;
		
		// cache instance
		m_instanceInfo = *info;
		m_instance = *instance;

		// create memory mapped files
		if (ovr_freepie_init() != 0)
			return XR_ERROR_HANDLE_INVALID;

		m_output->HmdMounted = true;

		return result;
	}

	XrResult xrNegotiateLoaderApiLayerInterface(const XrNegotiateLoaderInfo* loaderInfo, const char* layerName, XrNegotiateApiLayerRequest* apiLayerRequest)
	{
		_layerName = _strdup(layerName);

		// TODO: proper version check
		apiLayerRequest->layerInterfaceVersion = loaderInfo->maxInterfaceVersion;
		apiLayerRequest->layerApiVersion = loaderInfo->maxApiVersion;
		apiLayerRequest->getInstanceProcAddr = xrGetInstanceProcAddr;
		apiLayerRequest->createApiLayerInstance = xrCreateApiLayerInstance;

		return XR_SUCCESS;
	}
} // namespace openxr_api_layer