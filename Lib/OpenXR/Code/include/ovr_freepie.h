#ifndef OVR_FREEPIE_6DOF_H
#define OVR_FREEPIE_6DOF_H

#include <openxr.h>
#include <loader_interfaces.h>

typedef struct ovr_freepie_6dof
{
	float left[3];
	float up[3];
	float forward[3];
	float position[3];
} ovr_freepie_6dof;

typedef struct ovr_freepie_data {
	
	ovr_freepie_6dof head;
	ovr_freepie_6dof leftHand;
	ovr_freepie_6dof rightHand;

	float LeftTrigger;
	float RightTrigger;

	float LeftGrip;
	float RightGrip;

	float LeftStickAxes[2];
	float RightStickAxes[2];

	float LeftStick;
	float RightStick;

	float A;
	float B;
	float X;
	float Y;

	unsigned int StatusHead;
	unsigned int StatusLeftHand;
	unsigned int StatusRightHand;
	unsigned int HmdMounted;

} ovr_freepie_data;

int ovr_freepie_init();
int ovr_freepie_read(ovr_freepie_data *output);
int ovr_freepie_destroy();
int ovr_freepie_reset_orientation();
int ovr_freepie_configure_input(unsigned int inputConfig);
int ovr_freepie_trigger_haptic_pulse(unsigned int controllerIndex, float duration, float frequency, float amplitude);

namespace openxr_api_layer
{
	XrResult xrNegotiateLoaderApiLayerInterface(const XrNegotiateLoaderInfo* loaderInfo, const char* layerName, XrNegotiateApiLayerRequest* apiLayerRequest);
}
#endif