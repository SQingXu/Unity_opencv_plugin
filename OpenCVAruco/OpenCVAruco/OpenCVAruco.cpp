#include "pch.h"
#include "OpenCVAruco.h"
#include <vector>
#include <iostream>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/calib3d/calib3d.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/imgproc.hpp>
#include "LocatableCameraModule.h"
#include "Helper.h"

using namespace HololensCamera;
using namespace std;
using namespace cv;
using namespace Windows::Storage;

enum MatrixType{MarkerfOrigin = 0, HeadfCamera = 1, HeadfOrigin = 2, LeftToRight = 3};

std::shared_ptr<LocatableCameraModule> m_locatableCameraModule = nullptr;
std::shared_ptr<LocatableCameraFrame> m_locatableCameraFrame = nullptr;
std::vector<vector<Point2f>> markerCorners, rejectedCandidates;
Ptr<aruco::DetectorParameters> parameters;
std::vector<int> markerIds;
Ptr<aruco::Dictionary> dictionary = aruco::getPredefinedDictionary(aruco::DICT_6X6_250);

Mat camera_matrix;
Mat dist_coeff;
vector<Vec3d> rot_vecs, tranl_vecs;

Mat MfO;
Mat HfC;
Mat HfO;
Mat LeftRightMat;

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
						/*Mat bgr;
						cvtColor(mat, bgr, cv::COLOR_BGRA2BGR);
						Mat copy = bgr.clone();*/
						DirectX::SimpleMath::Matrix p_matrix = locatableCameraFrame->GetProjectionTransform();
						DirectX::SimpleMath::Matrix v_matrix = locatableCameraFrame->GetViewTransform();
						
						DebugInUnityMat(p_matrix);
						DebugInUnityMat(v_matrix);
						
						float fx = p_matrix._11 * (mat.cols / 2);
						float fy = p_matrix._22 * (mat.rows / 2);
						float cx = p_matrix._31 * (mat.cols / 2) + mat.cols / 2;
						float cy = p_matrix._32 * (mat.rows / 2) + mat.rows / 2;
						float c_data[] = { fx, 0, cx, 0, fy, cy, 0, 0, 1 }; 
						float d_data[] = { 0,0,0,0 };
						camera_matrix = Mat(3, 3, CV_32F, c_data);
						dist_coeff = Mat(1, 4, CV_32F, d_data);
						aruco::detectMarkers(mat, dictionary, markerCorners, markerIds);
						if (markerIds.size() > 0) {
							//DebugInUnity("Recognize more 0 markers");
							DebugInUnity(std::to_string(markerIds.at(0)));

							//Estimate pose MarkerfCamera translation and rotation vector;
							aruco::estimatePoseSingleMarkers(markerCorners, 0.054, camera_matrix,dist_coeff,rot_vecs, tranl_vecs);
							

							Vec3d rot_vec = rot_vecs.at(0);
							Vec3d tranl_vec = tranl_vecs.at(0);
							Mat rot_matrix = Mat(3,3,CV_64F);
							cv::Rodrigues(rot_vec, rot_matrix);
							double mfc_data[] = { rot_matrix.at<double>(0,0), rot_matrix.at<double>(0,1), rot_matrix.at<double>(0,2), tranl_vec[0],
								rot_matrix.at<double>(1,0), rot_matrix.at<double>(1,1), rot_matrix.at<double>(1,2), tranl_vec[1], 
								rot_matrix.at<double>(2,0), rot_matrix.at<double>(2,1), rot_matrix.at<double>(2,2), tranl_vec[2], 
								0, 0, 0, 1};
							Mat MarkerfCamera = Mat(4, 4, CV_64F, mfc_data);
							Mat CamerafMarker = MarkerfCamera.inv();
							DebugInUnity("Marker from camera");
							DebugInUnityMat<double>(MarkerfCamera);
							DebugInUnity("Camera from marker");
							DebugInUnityMat<double>(CamerafMarker);

							if (save) {
								aruco::drawDetectedMarkers(mat, markerCorners, markerIds);
								aruco::drawAxis(mat, camera_matrix, dist_coeff, rot_vecs, tranl_vecs, 0.1);

								Platform::String^ path = ApplicationData::Current->LocalCacheFolder->Path;
								std::wstring wstr(path->Begin());
								std::string path_str(wstr.begin(), wstr.end());
								std::string filename = "test_img.jpg";
								std::string filepath = (path_str + "/") + filename;
								DebugInUnity(path_str);
								cv::imwrite(filepath, mat);
								save = false;
							}
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

extern "C" __declspec(dllexport) void PassInMatrix(double* arr, int rows, int cols, int type) {
	double* mat_data = new double[rows*cols];
	std::copy(arr, arr + (rows*cols), mat_data);
	Mat mat(rows, cols, CV_64F, mat_data);
	if (type == (int)MatrixType::HeadfCamera) {
		HfC = mat;
		DebugInUnity("pass in headfcamera matrix");
		DebugInUnityMat<double>(HfC);

	}
	else if (type == (int)MatrixType::MarkerfOrigin) {
		MfO = mat;
		DebugInUnity("pass in markerforigin matrix");
		DebugInUnityMat<double>(MfO);
	}
	else if (type == (int)MatrixType::LeftToRight) {
		LeftRightMat = mat;
	}
}
