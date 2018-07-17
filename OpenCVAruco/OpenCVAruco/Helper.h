#pragma once
#include <Windows.Foundation.h>
#include <Windows.h>
#include <iostream>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <wrl/wrappers/corewrappers.h>
#include <wrl/client.h>
#include <MemoryBuffer.h>

using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Foundation;
using namespace Microsoft::WRL;
using namespace cv;

typedef void(__stdcall * DebugCallback) (const char * str);
extern DebugCallback gDebugCallback;

extern "C" VOID __declspec(dllexport) RegisterDebugCallback(DebugCallback callback);

void DebugInUnity(std::string message);
bool GetPointerToPixelData(SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity);
bool ConvertMat(SoftwareBitmap^ from, Mat& convertedMat);