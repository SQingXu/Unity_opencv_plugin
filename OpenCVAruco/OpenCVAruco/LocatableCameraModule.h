#pragma once
#include "pch.h"
#include "LocatableCameraFrame.h"


namespace HololensCamera {
	class LocatableCameraModule {
	public:
		static Concurrency::task<std::shared_ptr<LocatableCameraModule>> CreateAsync();
		LocatableCameraModule(
			Platform::Agile<Windows::Media::Capture::MediaCapture> mediaCapture,
			Windows::Media::Capture::Frames::MediaFrameReader^ reader,
			Windows::Media::Capture::Frames::MediaFrameSource^ source);
		std::shared_ptr<LocatableCameraFrame> GetFrame();

	private:
		void OnFrameArrived(
			Windows::Media::Capture::Frames::MediaFrameReader^ sender,
			Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs^ args);
		Platform::Guid StringToGuid(Platform::String^ str);
		DirectX::SimpleMath::Matrix IBoxArrayToMatrix(Platform::IBoxArray<uint8>^ array);

		Platform::Guid m_coordinateSystemGuid;
		Platform::Guid m_viewTransformGuid;
		Platform::Guid m_projectionTransformGuid;

		Platform::Agile<Windows::Media::Capture::MediaCapture> m_mediaCapture;
		Windows::Media::Capture::Frames::MediaFrameReader^ m_mediaFrameReader;

		std::shared_mutex m_propertiesLock;
		Windows::Media::Capture::Frames::MediaFrameSource^ m_mediaFrameSource;
		std::shared_ptr<LocatableCameraFrame> m_frame;
		uint32 m_frameId;
	};
}