#include "LocatableCameraModule.h"
#include "Helper.h"

namespace HololensCamera {
	LocatableCameraModule::LocatableCameraModule(
		Platform::Agile<Windows::Media::Capture::MediaCapture> mediaCapture,
		Windows::Media::Capture::Frames::MediaFrameReader^ reader,
		Windows::Media::Capture::Frames::MediaFrameSource^ source)
		: m_coordinateSystemGuid(LocatableCameraModule::StringToGuid(L"{9D13C82F-2199-4E67-91CD-D1A4181F2534}"))
		, m_viewTransformGuid(LocatableCameraModule::StringToGuid(L"{4E251FA4-830F-4770-859A-4B8D99AA809B}"))
		, m_projectionTransformGuid(LocatableCameraModule::StringToGuid(L"{47F9FCB5-2A02-4F26-A477-792FDF95886A}"))
		, m_mediaCapture(std::move(mediaCapture))
		, m_mediaFrameReader(std::move(reader))
		, m_mediaFrameSource(std::move(source))
		, m_frameId(0)
	{
		using Windows::Foundation::TypedEventHandler;
		using Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs;
		using Windows::Media::Capture::Frames::MediaFrameReader;
		using std::placeholders::_1;
		using std::placeholders::_2;

		m_mediaFrameReader->FrameArrived +=
			ref new TypedEventHandler<MediaFrameReader^, MediaFrameArrivedEventArgs^>(
				std::bind(&LocatableCameraModule::OnFrameArrived, this, _1, _2));
	}

	Concurrency::task<std::shared_ptr<LocatableCameraModule>> LocatableCameraModule::CreateAsync()
	{
		using Windows::Media::Capture::MediaCapture;
		using Windows::Media::Capture::MediaCaptureInitializationSettings;
		using Windows::Media::Capture::MediaCaptureMemoryPreference;
		using Windows::Media::Capture::StreamingCaptureMode;
		using Windows::Media::Capture::Frames::MediaFrameSource;
		using Windows::Media::Capture::Frames::MediaFrameSourceGroup;
		using Windows::Media::Capture::Frames::MediaFrameSourceInfo;
		using Windows::Media::Capture::Frames::MediaFrameSourceKind;
		using Windows::Media::Capture::Frames::MediaFrameReader;
		using Windows::Media::Capture::Frames::MediaFrameReaderStartStatus;

		return Concurrency::create_task(MediaFrameSourceGroup::FindAllAsync())
			.then([](Windows::Foundation::Collections::IVectorView<MediaFrameSourceGroup^>^ groups)
		{
			DebugInUnity("After FindAllSync(), reached selecting groups");
			std::cout << "After FindAllSync(), reached selecting groups" << std::endl;
			MediaFrameSourceGroup^ selectedGroup = nullptr;
			MediaFrameSourceInfo^ selectedSourceInfo = nullptr;
			bool image = false;

			// Pick first color source.
			for (auto sourceGroup : groups)
			{
				for (auto sourceInfo : sourceGroup->SourceInfos)
				{
					std::wstring wstr(sourceInfo->Id->Begin());
					std::string id_str(wstr.begin(), wstr.end());
					
					if (sourceInfo->SourceKind == MediaFrameSourceKind::Color)
					{
						selectedSourceInfo = sourceInfo;
						DebugInUnity("color id: " + id_str);
					}
					if (sourceInfo->SourceKind == MediaFrameSourceKind::Image) {
						image = true;
						DebugInUnity("image id: " + id_str);
					}
					DebugInUnity("common id: " + id_str);



				}

				if (selectedSourceInfo != nullptr && image)
				{
					selectedGroup = sourceGroup;
					DebugInUnity("Find the source group");
					break;
				}
			}

			// No valid camera was found. This will happen on the emulator.
			if (selectedGroup == nullptr || selectedSourceInfo == nullptr)
			{
				DebugInUnity("Didn't Find the source");
				return concurrency::task_from_result(std::shared_ptr<LocatableCameraModule>(nullptr));
			}

			auto settings = ref new MediaCaptureInitializationSettings();
			settings->MemoryPreference = MediaCaptureMemoryPreference::Cpu; // Need SoftwareBitmaps for FaceAnalysis
			settings->StreamingCaptureMode = StreamingCaptureMode::Video;   // Only need to stream video
			settings->SourceGroup = selectedGroup;

			Platform::Agile<MediaCapture> mediaCapture(ref new MediaCapture());
			DebugInUnity("settings for media capture are ready");
			return concurrency::create_task(mediaCapture->InitializeAsync(settings))
				.then([=]
			{
				DebugInUnity("After Initialize Async with settings");
				MediaFrameSource^ selectedSource = mediaCapture->FrameSources->Lookup(selectedSourceInfo->Id);

				return concurrency::create_task(mediaCapture->CreateFrameReaderAsync(selectedSource))
					.then([=](MediaFrameReader^ reader)
				{
					DebugInUnity("After create frame reader async");
					return concurrency::create_task(reader->StartAsync())
						.then([=](MediaFrameReaderStartStatus status)
					{
						// Only create a VideoFrameProcessor if the reader successfully started
						if (status == MediaFrameReaderStartStatus::Success)
						{
							DebugInUnity("After reader starting in success");
							return std::make_shared<LocatableCameraModule>(mediaCapture, reader, selectedSource);
						}
						else
						{
							DebugInUnity("Reader starting failed");
							return std::shared_ptr<LocatableCameraModule>(nullptr);
						}
					});
				});
			});
		});
	}


	std::shared_ptr<LocatableCameraFrame> LocatableCameraModule::GetFrame()
	{
		auto lock = std::shared_lock<std::shared_mutex>(m_propertiesLock);
		if (m_frame != nullptr) {
			//DebugInUnity("m_frame is not null");
		}
		return m_frame;
	}

	void LocatableCameraModule::OnFrameArrived(
		Windows::Media::Capture::Frames::MediaFrameReader^ sender,
		Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs^ args)
	{
		using Windows::Graphics::Imaging::BitmapPixelFormat;
		using Windows::Graphics::Imaging::SoftwareBitmap;
		using Windows::Media::Capture::Frames::MediaFrameReference;
		using Windows::Perception::Spatial::SpatialCoordinateSystem;

		if (MediaFrameReference^ frame = sender->TryAcquireLatestFrame())
		{
			//DebugInUnity("Try acquire");
			if (auto videoMediaFrame = frame->VideoMediaFrame)
			{
				//DebugInUnity("equals videomedia frame");
				if (auto softwareBitmap = videoMediaFrame->SoftwareBitmap)
				{
					//DebugInUnity("find bitmap");
					try
					{
						//DebugInUnity("handling frame arrived event");
						// Accessing properties can result in throwing an exception since mediaFrameReference can be disposed inside the VideoFrameProcessor.
						// Handling this part before performance demanding processes tends to prevent throw exceptions.

						// cameraSpatialCoordinateSystem only contains translation
						auto cameraSpatialCoordinateSystemObject(frame->Properties->Lookup(m_coordinateSystemGuid));
						auto cameraSpatialCoordinateSystem(safe_cast<SpatialCoordinateSystem^>(cameraSpatialCoordinateSystemObject));
						//// cameraViewTransform consists of rotation and translation
						auto cameraViewTransformBox(safe_cast<Platform::IBoxArray<uint8>^>(frame->Properties->Lookup(m_viewTransformGuid)));
						auto cameraProjectionTransformBox = safe_cast<Platform::IBoxArray<uint8>^>(frame->Properties->Lookup(m_projectionTransformGuid));
						auto cameraViewTransform(LocatableCameraModule::IBoxArrayToMatrix(cameraViewTransformBox));
						auto cameraProjectionTransform = LocatableCameraModule::IBoxArrayToMatrix(cameraProjectionTransformBox);

						//DebugInUnity("Start to convert");
						softwareBitmap = SoftwareBitmap::Convert(softwareBitmap, BitmapPixelFormat::Gray8);

						//DebugInUnity("Finish converting");
						{
							std::lock_guard<std::shared_mutex> lock(m_propertiesLock);
							//DebugInUnity("load frame");
							//DebugInUnity(std::to_string(m_frameId));
							m_frame = std::make_shared<LocatableCameraFrame>(
								++m_frameId, softwareBitmap, cameraSpatialCoordinateSystem,
								cameraViewTransform, cameraProjectionTransform);
						}
					}
					catch (Platform::InvalidCastException^ e)
					{
						//Logger::Log(L"InvalidCastException occured while using mediaFrameReference->Properties in VideoFrameProcessor::OnFrameArrived");
						//Logger::Log(e->Message);
						DebugInUnity("InvalidCastException occured while using mediaFrameReference->Properties in LocatableCameraModule::OnFrameArrived");
						//DebugInUnity(e->Message);
						// Remove this exception throw when not debugging.
						throw e;
					}
				}
			}

		}
	}

	
	Platform::Guid LocatableCameraModule::StringToGuid(Platform::String^ str) {
		GUID rawguid;
		HRESULT hr = IIDFromString(str->Data(), &rawguid);
		if (SUCCEEDED(hr)) {
			Platform::Guid guid(rawguid);
			return guid;
		}

		throw new std::exception("failed to create Guid");
	}

	DirectX::SimpleMath::Matrix LocatableCameraModule::IBoxArrayToMatrix(Platform::IBoxArray<uint8>^ array)
	{
		float* matrixData = reinterpret_cast<float*>(array->Value->Data);
		return DirectX::SimpleMath::Matrix(
			matrixData[0], matrixData[1], matrixData[2], matrixData[3],
			matrixData[4], matrixData[5], matrixData[6], matrixData[7],
			matrixData[8], matrixData[9], matrixData[10], matrixData[11],
			matrixData[12], matrixData[13], matrixData[14], matrixData[15]);
	}
}