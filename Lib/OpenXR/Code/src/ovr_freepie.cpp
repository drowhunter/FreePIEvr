extern "C"
{
  #include "../include/ovr_freepie.h"
}

//#define WITH_GRAPHICS

#ifdef WITH_GRAPHICS
#define XR_USE_GRAPHICS_API_OPENGL
#define XR_USE_PLATFORM_WIN32
#endif

#include <openxr.h>
#include <string.h>

#include "GraphicsAPI.h"
#ifdef WITH_GRAPHICS
#include "GraphicsAPI_D3D11.h"
#endif

constexpr static XrFormFactor m_formFactor{XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY};

XrInstance m_instance;
XrSystemId m_system;
XrSession m_session;

#ifdef WITH_GRAPHICS
std::unique_ptr<GraphicsAPI> m_graphicsAPI = nullptr;
#endif

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

XrPath CreateXrPath(const char* path_string) {
	XrPath xrPath;
	XrResult result = xrStringToPath(m_instance, path_string, &xrPath);
	if (XR_SUCCEEDED(result))
		return xrPath;

	return XR_NULL_PATH;
}
std::string FromXrPath(XrPath path) {
	uint32_t strl;
	char text[XR_MAX_PATH_LENGTH];
	XrResult result = xrPathToString(m_instance, path, XR_MAX_PATH_LENGTH, &strl, text);
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
	strncpy(actionCI.actionName, name, XR_MAX_ACTION_NAME_SIZE);
	// Localized names are required so there is a human-readable action name to show the user if they are rebinding the Action in an options screen.
	strncpy(actionCI.localizedActionName, name, XR_MAX_LOCALIZED_ACTION_NAME_SIZE);
	XrResult result = xrCreateAction(m_actionSet, &actionCI, &xrAction);

	return XR_SUCCEEDED(result);
};

bool SuggestBindings(const char* profile_path, std::vector<XrActionSuggestedBinding> bindings)
{
	// The application can call xrSuggestInteractionProfileBindings once per interaction profile that it supports.
	XrInteractionProfileSuggestedBinding interactionProfileSuggestedBinding{ XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING };
	interactionProfileSuggestedBinding.interactionProfile = CreateXrPath(profile_path);
	interactionProfileSuggestedBinding.suggestedBindings = bindings.data();
	interactionProfileSuggestedBinding.countSuggestedBindings = (uint32_t)bindings.size();
	XrResult result = xrSuggestInteractionProfileBindings(m_instance, &interactionProfileSuggestedBinding);

	return XR_SUCCEEDED(result);
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
	XrResult result = xrCreateActionSpace(m_session, &actionSpaceCI, &xrSpace);
	if (XR_SUCCEEDED(result))
		return xrSpace;

	return 0;
};

int ovr_freepie_init()
{
	// graphics extension requirement
	static const char* const extensionNames[] = {
#ifdef WITH_GRAPHICS
		XR_KHR_D3D11_ENABLE_EXTENSION_NAME
#else
		XR_MND_HEADLESS_EXTENSION_NAME
#endif
	};

	// create instance
	XrInstanceCreateInfo instanceInfo{ XR_TYPE_INSTANCE_CREATE_INFO };
	instanceInfo.applicationInfo = { "FreePIE", 1, "", 1, XR_CURRENT_API_VERSION };
	instanceInfo.enabledExtensionCount = 1;
	instanceInfo.enabledExtensionNames = extensionNames;
	XrResult result = xrCreateInstance(&instanceInfo, &m_instance);
	if (XR_SUCCEEDED(result) == false)
		return result;

	// get system
	XrSystemGetInfo systemInfo{ XR_TYPE_SYSTEM_GET_INFO };
	systemInfo.formFactor = m_formFactor;
	result = xrGetSystem(m_instance, &systemInfo, &m_system);
	if (XR_SUCCEEDED(result) == false)
		return 90;


#ifdef WITH_GRAPHICS
	// create graphics (not really used)
	m_graphicsAPI = std::make_unique<GraphicsAPI_D3D11>(m_instance, m_system);
#endif

	// finally create session for interaction
	XrSessionCreateInfo sessionInfo{ XR_TYPE_SESSION_CREATE_INFO };
	sessionInfo.systemId = m_system;
#ifdef WITH_GRAPHICS
	sessionInfo.next = m_graphicsAPI->GetGraphicsBinding();
#endif
	result = xrCreateSession(m_instance, &sessionInfo, &m_session);
	if (XR_SUCCEEDED(result) == false)
		return 91;

	// create action set
	XrActionSetCreateInfo actionSetCI{ XR_TYPE_ACTION_SET_CREATE_INFO };
	strncpy(actionSetCI.actionSetName, "freepie-actionset", XR_MAX_ACTION_SET_NAME_SIZE);
	strncpy(actionSetCI.localizedActionSetName, "FreePIE", XR_MAX_LOCALIZED_ACTION_SET_NAME_SIZE);
	result = xrCreateActionSet(m_instance, &actionSetCI, &m_actionSet);
	if (XR_SUCCEEDED(result) == false)
		return 92;
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
	
	// For later convenience we create the XrPaths for the subaction path names.
	m_handPaths[0] = CreateXrPath("/user/hand/left");
	m_handPaths[1] = CreateXrPath("/user/hand/right");

	bool any_ok = false;
	// general
	any_ok |= SuggestBindings("/interaction_profiles/khr/simple_controller", {{m_gripAction, CreateXrPath("/user/hand/left/input/select/click")},
																			  {m_gripAction, CreateXrPath("/user/hand/right/input/select/click")},
																			  {m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose")},
																			  {m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose")},
																			  {m_hapticAction, CreateXrPath("/user/hand/left/output/haptic")},
																			  {m_hapticAction, CreateXrPath("/user/hand/right/output/haptic")} });
	if (any_ok == false)
			return 200;

	// htc vive
	any_ok = false;
	any_ok |= SuggestBindings("/interaction_profiles/htc/vive_controller", {{m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/click")},
																			{m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/click")},
																			{m_stickXAction, CreateXrPath("/user/hand/left/input/trackpad/x")},
																			{m_stickXAction, CreateXrPath("/user/hand/right/input/trackpad/x")},
																			{m_stickYAction, CreateXrPath("/user/hand/left/input/trackpad/y")},
																			{m_stickYAction, CreateXrPath("/user/hand/right/input/trackpad/y")},
																			{m_stickClickAction, CreateXrPath("/user/hand/left/input/trackpad/click")},
																			{m_stickClickAction, CreateXrPath("/user/hand/right/input/trackpad/click")},
																			{m_stickTouchAction, CreateXrPath("/user/hand/left/input/trackpad/touch")},
																			{m_stickTouchAction, CreateXrPath("/user/hand/right/input/trackpad/touch")},
																			{m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value")},
																			{m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value")},
																			{m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose")},
																			{m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose")},
																			{m_hapticAction, CreateXrPath("/user/hand/left/output/haptic")},
																			{m_hapticAction, CreateXrPath("/user/hand/right/output/haptic")} });
	if (any_ok == false)
		return 201;


	// mxr
	any_ok = false;
	any_ok |= SuggestBindings("/interaction_profiles/microsoft/motion_controller", { {m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/click")},
																					 {m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/click")},
																					 {m_stickXAction, CreateXrPath("/user/hand/left/input/trackpad/x")},
																					 {m_stickXAction, CreateXrPath("/user/hand/right/input/trackpad/x")},
																					 {m_stickYAction, CreateXrPath("/user/hand/left/input/trackpad/y")},
																					 {m_stickYAction, CreateXrPath("/user/hand/right/input/trackpad/y")},
																					 {m_stickClickAction, CreateXrPath("/user/hand/left/input/trackpad/click")},
																					 {m_stickClickAction, CreateXrPath("/user/hand/right/input/trackpad/click")},
																					 {m_stickTouchAction, CreateXrPath("/user/hand/left/input/trackpad/touch")},
																					 {m_stickTouchAction, CreateXrPath("/user/hand/right/input/trackpad/touch")},
																					 {m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value")},
																					 {m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value")},
																					 {m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose")},
																					 {m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose")},
																					 {m_hapticAction, CreateXrPath("/user/hand/left/output/haptic")},
																					 {m_hapticAction, CreateXrPath("/user/hand/right/output/haptic")} });
	if (any_ok == false)
		return 202;

	// oculus
	any_ok = false;
	any_ok |= SuggestBindings("/interaction_profiles/oculus/touch_controller", {{m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/value")},
																				{m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/value")},
																				{m_aClickAction, CreateXrPath("/user/hand/left/input/x/click")},
																				{m_aClickAction, CreateXrPath("/user/hand/right/input/a/click")},
																				{m_aTouchAction, CreateXrPath("/user/hand/left/input/x/touch")},
																				{m_aTouchAction, CreateXrPath("/user/hand/right/input/a/touch")},
																				{m_bClickAction, CreateXrPath("/user/hand/left/input/y/click")},
																				{m_bClickAction, CreateXrPath("/user/hand/right/input/b/click")},
																				{m_bTouchAction, CreateXrPath("/user/hand/left/input/y/touch")},
																				{m_bTouchAction, CreateXrPath("/user/hand/right/input/b/touch")},
																				{m_stickXAction, CreateXrPath("/user/hand/left/input/thumbstick/x")},
																				{m_stickXAction, CreateXrPath("/user/hand/right/input/thumbstick/x")},
																				{m_stickYAction, CreateXrPath("/user/hand/left/input/thumbstick/y")},
																				{m_stickYAction, CreateXrPath("/user/hand/right/input/thumbstick/y")},
																				{m_stickClickAction, CreateXrPath("/user/hand/left/input/thumbstick/click")},
																				{m_stickClickAction, CreateXrPath("/user/hand/right/input/thumbstick/click")},
																				{m_stickTouchAction, CreateXrPath("/user/hand/left/input/thumbstick/touch")},
																				{m_stickTouchAction, CreateXrPath("/user/hand/right/input/thumbstick/touch")},
																				{m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value")},
																				{m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value")},
																				{m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose")},
																				{m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose")},
																				{m_hapticAction, CreateXrPath("/user/hand/left/output/haptic")},
																				{m_hapticAction, CreateXrPath("/user/hand/right/output/haptic")} });
	if (any_ok == false)
		return 203;


	// valve index
	any_ok = false;
	any_ok |= SuggestBindings("/interaction_profiles/valve/index_controller", { {m_gripAction, CreateXrPath("/user/hand/left/input/squeeze/value")},
																				{m_gripAction, CreateXrPath("/user/hand/right/input/squeeze/value")},
																				{m_aClickAction, CreateXrPath("/user/hand/left/input/a/click")},
																				{m_aClickAction, CreateXrPath("/user/hand/right/input/a/click")},
																				{m_aTouchAction, CreateXrPath("/user/hand/left/input/a/touch")},
																				{m_aTouchAction, CreateXrPath("/user/hand/right/input/a/touch")},
																				{m_bClickAction, CreateXrPath("/user/hand/left/input/b/click")},
																				{m_bClickAction, CreateXrPath("/user/hand/right/input/b/click")},
																				{m_bTouchAction, CreateXrPath("/user/hand/left/input/b/touch")},
																				{m_bTouchAction, CreateXrPath("/user/hand/right/input/b/touch")},
																				{m_stickXAction, CreateXrPath("/user/hand/left/input/thumbstick/x")},
																				{m_stickXAction, CreateXrPath("/user/hand/right/input/thumbstick/x")},
																				{m_stickYAction, CreateXrPath("/user/hand/left/input/thumbstick/y")},
																				{m_stickYAction, CreateXrPath("/user/hand/right/input/thumbstick/y")},
																				{m_stickClickAction, CreateXrPath("/user/hand/left/input/thumbstick/click")},
																				{m_stickClickAction, CreateXrPath("/user/hand/right/input/thumbstick/click")},
																				{m_stickTouchAction, CreateXrPath("/user/hand/left/input/thumbstick/touch")},
																				{m_stickTouchAction, CreateXrPath("/user/hand/right/input/thumbstick/touch")},
																				{m_triggerAction, CreateXrPath("/user/hand/left/input/trigger/value")},
																				{m_triggerAction, CreateXrPath("/user/hand/right/input/trigger/value")},
																				{m_handPoseAction, CreateXrPath("/user/hand/left/input/aim/pose")},
																				{m_handPoseAction, CreateXrPath("/user/hand/right/input/aim/pose")},
																				{m_hapticAction, CreateXrPath("/user/hand/left/output/haptic")},
																				{m_hapticAction, CreateXrPath("/user/hand/right/output/haptic")} });
	if (any_ok == false)
		return 204;

	// Fill out an XrReferenceSpaceCreateInfo structure and create a reference XrSpace, specifying a Local space with an identity pose as the origin.
	XrReferenceSpaceCreateInfo referenceSpaceCI{ XR_TYPE_REFERENCE_SPACE_CREATE_INFO };
	referenceSpaceCI.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_LOCAL;
	referenceSpaceCI.poseInReferenceSpace = { {0.0f, 0.0f, 0.0f, 1.0f}, {0.0f, 0.0f, 0.0f} };
	result = xrCreateReferenceSpace(m_session, &referenceSpaceCI, &m_localSpace);
	if (XR_SUCCEEDED(result) == false)
		return 300;

	m_handPoseSpace[0] = CreateActionPoseSpace(m_handPoseAction, "/user/hand/left");
	m_handPoseSpace[1] = CreateActionPoseSpace(m_handPoseAction, "/user/hand/right");

	XrReferenceSpaceCreateInfo headPoseSpaceInfo = {
		XR_TYPE_REFERENCE_SPACE_CREATE_INFO,
		NULL,
		XR_REFERENCE_SPACE_TYPE_VIEW,
		{ {0, 0, 0, 1}, {0, 0, 0} }
	};
	result = xrCreateReferenceSpace(m_session, &headPoseSpaceInfo, &m_headPoseSpace);
	if (XR_SUCCEEDED(result) == false)
		return 301;

	// Attach the action set we just made to the session. We could attach multiple action sets!
	XrSessionActionSetsAttachInfo actionSetAttachInfo{ XR_TYPE_SESSION_ACTION_SETS_ATTACH_INFO };
	actionSetAttachInfo.countActionSets = 1;
	actionSetAttachInfo.actionSets = &m_actionSet;
	result = xrAttachSessionActionSets(m_session, &actionSetAttachInfo);
	if (XR_SUCCEEDED(result) == false)
		return 302;

	return 0;
}

int ovr_freepie_destroy()
{
	XrResult sessionResult = XR_SUCCESS;
	if (m_session)
		sessionResult = xrDestroySession(m_session);
	if (m_instance)
	{
		XrResult result = xrDestroyInstance(m_instance);
		if (XR_SUCCEEDED(result) == false)
			return result;
	}

	return sessionResult;
}

int ovr_freepie_reset_orientation()
{
	//ovr_RecenterTrackingOrigin(HMD);
	return 0;
}

int ovr_freepie_setPose(XrPosef* pose, ovr_freepie_6dof *dof)
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
	float s = x*x + y*y + z*z + w*w;
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

int ovr_freepie_read(ovr_freepie_data *output)
{
	// Poll OpenXR for a new event.
	XrEventDataBuffer eventData{ XR_TYPE_EVENT_DATA_BUFFER };
	auto XrPollEvents = [&]() -> bool {
		eventData = { XR_TYPE_EVENT_DATA_BUFFER };
		return xrPollEvent(m_instance, &eventData) == XR_SUCCESS;
	};

	XrResult result;
	while (XrPollEvents()) {
		switch (eventData.type) {
			// Log that the interaction profile has changed.
		case XR_TYPE_EVENT_DATA_INTERACTION_PROFILE_CHANGED: 
			{
				XrEventDataInteractionProfileChanged* interactionProfileChanged = reinterpret_cast<XrEventDataInteractionProfileChanged*>(&eventData);
				if (interactionProfileChanged->session != m_session) {
					break;
				}

				output->StatusHead = 1;
				break;
			}
		case XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED:
			{
				XrEventDataSessionStateChanged* sessionStateChanged = reinterpret_cast<XrEventDataSessionStateChanged*>(&eventData);
				if (sessionStateChanged->session != m_session) {
					break;
				}

				if (sessionStateChanged->state == XR_SESSION_STATE_READY)
				{
					// SessionState is ready. Begin the XrSession using the XrViewConfigurationType.
					XrSessionBeginInfo sessionBeginInfo{ XR_TYPE_SESSION_BEGIN_INFO };
					sessionBeginInfo.primaryViewConfigurationType = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
					result = xrBeginSession(m_session, &sessionBeginInfo);
					output->HmdMounted = XR_SUCCEEDED(result);
				}
				if (sessionStateChanged->state == XR_SESSION_STATE_STOPPING)
				{
					// SessionState is stopping. End the XrSession.
					result = xrEndSession(m_session);
					output->HmdMounted = false;
				}
				break;
			}
		}
	}

	if (output->HmdMounted)
	{

#ifdef WITH_GRAPHICS
		XrFrameState frameState{ XR_TYPE_FRAME_STATE };
		XrFrameWaitInfo frameWaitInfo{ XR_TYPE_FRAME_WAIT_INFO };
		result = xrWaitFrame(m_session, &frameWaitInfo, &frameState);
		if (XR_SUCCEEDED(result) == false)
			return 3;
		
		// Tell the OpenXR compositor that the application is beginning the frame.
		XrFrameBeginInfo frameBeginInfo{ XR_TYPE_FRAME_BEGIN_INFO };
		result = xrBeginFrame(m_session, &frameBeginInfo);
		if (XR_SUCCEEDED(result) == false)
			return 4;

		XrTime predictedTime = frameState.predictedDisplayTime;
#else
		XrTime predictedTime = 1;
#endif

		// Update our action set with up-to-date input data.
		// First, we specify the actionSet we are polling.
		XrActiveActionSet activeActionSet{};
		activeActionSet.actionSet = m_actionSet;
		activeActionSet.subactionPath = XR_NULL_PATH;
		// Now we sync the Actions to make sure they have current data.
		XrActionsSyncInfo actionsSyncInfo{ XR_TYPE_ACTIONS_SYNC_INFO };
		actionsSyncInfo.countActiveActionSets = 1;
		actionsSyncInfo.activeActionSets = &activeActionSet;
		result = xrSyncActions(m_session, &actionsSyncInfo);
		if (XR_SUCCEEDED(result) == false)
			return 1;

		// get actions
		XrActionStateGetInfo actionStateGetInfo{ XR_TYPE_ACTION_STATE_GET_INFO };

		// hand poses
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_handPoseAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStatePose(m_session, &actionStateGetInfo, &m_handPoseState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 100;

			if (m_handPoseState[i].isActive) {
				XrSpaceLocation spaceLocation{ XR_TYPE_SPACE_LOCATION };
				XrResult res = xrLocateSpace(m_handPoseSpace[i], m_localSpace, predictedTime, &spaceLocation);
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
		output->StatusLeftHand = m_handPoseState[0].isActive ? 1 : 0;
		output->StatusRightHand = m_handPoseState[0].isActive ? 1 : 0;
		ovr_freepie_setPose(&m_handPose[0], &output->leftHand);
		ovr_freepie_setPose(&m_handPose[1], &output->rightHand);

		// trigger
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_triggerAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 101;
		}
		output->LeftTrigger = m_floatState[0].currentState;
		output->RightTrigger = m_floatState[1].currentState;

		// grip
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_gripAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 102;
		}
		output->LeftGrip = m_floatState[0].currentState;
		output->RightGrip = m_floatState[1].currentState;

		// sticks
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickXAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 103;
		}
		output->LeftStickAxes[0] = m_floatState[0].currentState;
		output->RightStickAxes[0] = m_floatState[1].currentState;

		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickYAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateFloat(m_session, &actionStateGetInfo, &m_floatState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 104;
		}
		output->LeftStickAxes[1] = m_floatState[0].currentState;
		output->RightStickAxes[1] = m_floatState[1].currentState;

		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 105;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_stickTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 106;
		}
		output->LeftStick = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		output->RightStick = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);

		// a button
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_aClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 107;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_aTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 108;
		}
		output->X = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		output->A = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);

		// b button
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_bClickAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_clickState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 109;
		}
		for (int i = 0; i < 2; i++) {
			actionStateGetInfo.action = m_bTouchAction;
			actionStateGetInfo.subactionPath = m_handPaths[i];
			result = xrGetActionStateBoolean(m_session, &actionStateGetInfo, &m_touchState[i]);
			if (XR_SUCCEEDED(result) == false)
				return 110;
		}
		output->Y = m_clickState[0].currentState ? 1.0f : (m_touchState[0].currentState ? 0.5f : 0.0f);
		output->B = m_clickState[1].currentState ? 1.0f : (m_touchState[1].currentState ? 0.5f : 0.0f);


		// head position
		{
			XrSpaceLocation spaceLocation = { XR_TYPE_SPACE_LOCATION, NULL, 0, {{0, 0, 0, 1}, {0, 0, 0}} };
			result = xrLocateSpace(m_headPoseSpace, m_localSpace, predictedTime, &spaceLocation);
			if (XR_SUCCEEDED(result) == false)
				return 111;

			if ((spaceLocation.locationFlags & XR_SPACE_LOCATION_POSITION_VALID_BIT) != 0 &&
				(spaceLocation.locationFlags & XR_SPACE_LOCATION_ORIENTATION_VALID_BIT) != 0) {

				ovr_freepie_setPose(&spaceLocation.pose, &output->head);
				output->StatusHead = 1;
			}
			else
			{
				output->StatusHead = 0;
			}
		}


#ifdef WITH_GRAPHICS
		// Tell OpenXR that we are finished with this frame; specifying its display time, environment blending and layers.
		XrFrameEndInfo frameEndInfo{ XR_TYPE_FRAME_END_INFO };
		frameEndInfo.displayTime = frameState.predictedDisplayTime;
		frameEndInfo.layerCount = 0;
		result = xrEndFrame(m_session, &frameEndInfo);
		if (XR_SUCCEEDED(result) == false)
			return 5;
#endif
	}
	return 0;
}
int ovr_freepie_trigger_haptic_pulse(unsigned int controllerIndex, float duration, float frequency, float amplitude)
{
	XrHapticVibration vibration{ XR_TYPE_HAPTIC_VIBRATION };
	vibration.amplitude = amplitude;
	vibration.duration = XR_MIN_HAPTIC_DURATION; // std::max(XR_MIN_HAPTIC_DURATION, (int)(1000 * 1000 * duration));
	vibration.frequency = XR_FREQUENCY_UNSPECIFIED;

	XrHapticActionInfo hapticActionInfo{ XR_TYPE_HAPTIC_ACTION_INFO };
	hapticActionInfo.action = m_hapticAction;
	hapticActionInfo.subactionPath = m_handPaths[controllerIndex];
	XrResult result = xrApplyHapticFeedback(m_session, &hapticActionInfo, (XrHapticBaseHeader*)&vibration);
	if (XR_SUCCEEDED(result) == false)
		return 1;

	return 0;
}