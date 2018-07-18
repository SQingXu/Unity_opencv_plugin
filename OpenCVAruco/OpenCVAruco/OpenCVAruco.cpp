#include "pch.h"
#include "OpenCVAruco.h"
#include <vector>
#include <iostream>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/imgproc.hpp>
#include "LocatableCameraModule.h"
#include "Helper.h"

using namespace HololensCamera;
using namespace std;
using namespace cv;
using namespace Windows::Storage;

std::shared_ptr<LocatableCameraModule> m_locatableCameraModule = nullptr;
std::shared_ptr<LocatableCameraFrame> m_locatableCameraFrame = nullptr;
std::vector<vector<Point2f>> markerCorners, rejectedCandidates;
Ptr<aruco::DetectorParameters> parameters;
std::vector<int> markerIds;
Ptr<aruco::Dictionary> dictionary = aruco::getPredefinedDictionary(aruco::DICT_6X6_250);

bool save = true;


//int main() {
//	cv::Mat image;
//	cv::imshow("Display", image);
//}

extern "C"
{
	__declspec(dllexport) void StartCameraModule() {
		LocatableCameraModule::CreateAsync().then([](std::shared_ptr<LocatableCameraModule> module){
			DebugInUnity("Finish with create Async function");
			m_locatableCameraModule = std::move(module);
			/*if (m_locatableCameraModule != nullptr) {
				auto locatableCameraFrame = m_locatableCameraModule->GetFrame();

				if (locatableCameraFrame != nullptr)
				{
					int id = locatableCameraFrame->GetId();
					if (id % 100 == 0 && id != m_locatableCameraFrame->GetId())
					{
						std::cout << "Found a locatable camera frame with ID " << id << "!" << std::endl;
					}
				}
				else
				{
					std::cout << "Locatable camera frame is not available yet." << std::endl;
				}

				m_locatableCameraFrame = locatableCameraFrame;

			}*/
		});
		DebugInUnity("Hello from DLL");
	}

	
}

extern "C" __declspec(dllexport) void DetectMarkersAruco() {
		if (m_locatableCameraModule != nullptr) {
			Mat mat;
			auto locatableCameraFrame = m_locatableCameraModule->GetFrame();
			if (locatableCameraFrame != nullptr) {
				if (ConvertMat(locatableCameraFrame->GetSoftwareBitmap(), mat)) {
					try {
						if (save) {
							Platform::String^ path = ApplicationData::Current->LocalCacheFolder->Path;
							std::wstring wstr(path->Begin());
							std::string path_str(wstr.begin(),wstr.end());
							std::string filename = "test_img.jpg";
							std::string filepath = (path_str + "/") + filename;
							DebugInUnity(path_str);
							cv::imwrite(filepath, mat);
							save = false;
						}
						/*Mat bgr;
						cvtColor(mat, bgr, cv::COLOR_BGRA2BGR);
						Mat copy = bgr.clone();*/
						aruco::detectMarkers(mat, dictionary, markerCorners, markerIds);
						if (markerIds.size() > 0) {
							//DebugInUnity("Recognize more 0 markers");
							DebugInUnity(std::to_string(markerIds.at(0)));
						}
					}
					catch (cv::Exception& e) {
						DebugInUnity(e.msg);
					}
					
				}
			}else {
				DebugInUnity("frame is not valid");
			}
			m_locatableCameraFrame = locatableCameraFrame;
		}
	}
