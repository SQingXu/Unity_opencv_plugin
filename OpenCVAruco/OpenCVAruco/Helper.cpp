#include "Helper.h"

DebugCallback gDebugCallback;

extern "C" VOID __declspec(dllexport) RegisterDebugCallback(DebugCallback callback) {
	if(callback){
		gDebugCallback = callback;
	}
}

void DebugInUnity(std::string message) {
	if (gDebugCallback) {
		gDebugCallback(message.c_str());
	}
}

bool GetPointerToPixelData(SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity) {
	BitmapBuffer^ bmpBuffer = bitmap->LockBuffer(BitmapBufferAccessMode::ReadWrite);
	IMemoryBufferReference^ reference = bmpBuffer->CreateReference();
	ComPtr<IMemoryBufferByteAccess> pBufferByteAccess;
	if ((reinterpret_cast<IInspectable*>(reference)->QueryInterface(IID_PPV_ARGS(&pBufferByteAccess))) != S_OK)
	{
		return false;
	}
	if (pBufferByteAccess->GetBuffer(pPixelData, capacity) != S_OK)
	{
		return false;
	}
	return true;

}
bool ConvertMat(SoftwareBitmap^ from, Mat& convertedMat) {
	unsigned char* pPixels = nullptr;
	unsigned int capacity = 0;
	if (!GetPointerToPixelData(from, &pPixels, &capacity)) {
		return false;
	}
	//DebugInUnity("width: " + std::to_string(from->PixelWidth));
	//DebugInUnity("height: " + std::to_string(from->PixelHeight));
	Mat mat(from->PixelHeight,
		from->PixelWidth,
		CV_8UC1,
		(void*)pPixels);

	convertedMat = mat;
	return true;
}