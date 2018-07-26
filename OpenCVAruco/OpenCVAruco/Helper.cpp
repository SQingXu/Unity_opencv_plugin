#include "Helper.h"

DebugCallback gDebugCallback;
PassMatrix4x4Callback gPassMatrix4x4Callback;

extern "C" VOID __declspec(dllexport) RegisterDebugCallback(DebugCallback callback) {
	if(callback){
		gDebugCallback = callback;
	}
}

extern "C" VOID __declspec(dllexport) RegisterPassMatrix4x4Callback(PassMatrix4x4Callback callback) {
	if (callback) {
		gPassMatrix4x4Callback = callback;
	}
}

void PassMatrix4x4(Mat mat, int mtype) {
	if (gPassMatrix4x4Callback && mat.rows == 4 && mat.cols == 4 && mat.type() == CV_64F) {
		gPassMatrix4x4Callback((double *)mat.data, mtype);
	}
}

void DebugInUnity(std::string message) {
	if (gDebugCallback) {
		gDebugCallback(message.c_str());
	}
}

void DebugInUnityMat(Matrix mat) {
	DebugInUnity(std::to_string(mat._11) + " " + std::to_string(mat._12) + " " + std::to_string(mat._13) + " " + std::to_string(mat._14) + '\n' +
		std::to_string(mat._21) + " " + std::to_string(mat._22) + " " + std::to_string(mat._23) + " " + std::to_string(mat._24) + '\n' +
		std::to_string(mat._31) + " " + std::to_string(mat._32) + " " + std::to_string(mat._33) + " " + std::to_string(mat._34) + '\n' +
		std::to_string(mat._41) + " " + std::to_string(mat._42) + " " + std::to_string(mat._43) + " " + std::to_string(mat._44));
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

Platform::Guid StringToGuid(Platform::String^ str) {
	GUID rawguid;
	HRESULT hr = IIDFromString(str->Data(), &rawguid);
	if (SUCCEEDED(hr)) {
		Platform::Guid guid(rawguid);
		return guid;
	}

	throw new std::exception("failed to create Guid");
}

Mat IBoxArrayToMatrix(Platform::IBoxArray<uint8>^ array)
{
	float* matrixData = reinterpret_cast<float*>(array->Value->Data);
	float* matrixDataCopy = new float[16];
	std::copy(matrixData, matrixData + 16, matrixDataCopy);
	return Mat(4, 4, CV_32F, matrixDataCopy);
}