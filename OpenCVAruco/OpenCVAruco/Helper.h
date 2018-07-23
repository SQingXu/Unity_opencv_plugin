#pragma once
#include "pch.h"
#include <Windows.Foundation.h>
#include <Windows.h>
#include <iostream>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <wrl/wrappers/corewrappers.h>
#include <wrl/client.h>
#include <MemoryBuffer.h>
#include <SimpleMath.h>

using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Foundation;
using namespace Microsoft::WRL;
using namespace DirectX::SimpleMath;
using namespace cv;

typedef void(__stdcall * DebugCallback) (const char * str);
extern DebugCallback gDebugCallback;

typedef void(__stdcall * PassMatrix4x4Callback) (double * arr);
extern PassMatrix4x4Callback gPassMatrix4x4Callback;

extern "C" VOID __declspec(dllexport) RegisterDebugCallback(DebugCallback callback);
extern "C" VOID __declspec(dllexport) RegisterPassMatrix4x4Callback(PassMatrix4x4Callback callback);

void DebugInUnity(std::string message);
void PassMatrix4x4(Mat mat);

template<class T>
void DebugInUnityMat(Mat mat) {
	int cols = mat.cols;
	int rows = mat.rows;
	std::string output = "";
	for (int i = 0; i < rows; i++) {
		for (int j = 0; j < cols; j++) {
			output += std::to_string(mat.at<T>(i, j));
			if (j != cols - 1) {
				output += " ";
			}
		}
		if (i != rows - 1) {
			output += '\n';
		}
	}
	DebugInUnity(output);
}

void DebugInUnityMat(Matrix mat);

bool GetPointerToPixelData(SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity);
bool ConvertMat(SoftwareBitmap^ from, Mat& convertedMat);