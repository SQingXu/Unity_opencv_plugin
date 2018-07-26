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

enum MatrixType{MarkerfOrigin = 0, HeadfCamera = 1, HeadfOrigin = 2, RightToLeft = 3, ViewMatrix = 4};

std::shared_ptr<LocatableCameraModule> m_locatableCameraModule = nullptr;
std::shared_ptr<LocatableCameraFrame> m_locatableCameraFrame = nullptr;
std::vector<vector<Point2f>> markerCorners, rejectedCandidates;
Ptr<aruco::DetectorParameters> parameters;
std::vector<int> markerIds;
Ptr<aruco::Dictionary> dictionary = aruco::getPredefinedDictionary(aruco::DICT_6X6_250);

bool first_frame = true;
Mat camera_matrix;
Mat dist_coeff;
vector<Vec3d> rot_vecs, tranl_vecs;

Mat MarkerfOriginMat;
Mat HeadfCameraMat;
Mat HeadfOriginMat;
Mat RightToLeftMat;

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
	}

	
}

extern "C" __declspec(dllexport) void DetectMarkersAruco() {
		if (m_locatableCameraModule != nullptr) {
			Mat mat;
			{
				
				auto locatableCameraFrame = m_locatableCameraModule->GetFrame();
				std::lock_guard<std::shared_mutex> lock(m_locatableCameraModule->m_propertiesLock);
				if (locatableCameraFrame != nullptr) {
					if (ConvertMat(locatableCameraFrame->GetSoftwareBitmap(), mat)) {
						if (first_frame) {
							Mat v_matrix = locatableCameraFrame->GetViewTransform();
							PassMatrix4x4(v_matrix, (int)MatrixType::ViewMatrix);
							DebugInUnityMat<double>(v_matrix);

							Mat p_matrix = locatableCameraFrame->GetProjectionTransform();
							double fx = p_matrix.at<float>(0,0) * (mat.cols / 2);
							double fy = p_matrix.at<float>(1,1) * (mat.rows / 2);
							double cx = p_matrix.at<float>(2,0) * (mat.cols / 2) + mat.cols / 2;
							double cy = p_matrix.at<float>(2,1) * (mat.rows / 2) + mat.rows / 2;
							double c_data[] = { fx, 0, cx, 0, fy, cy, 0, 0, 1 };
							double* c_ptr = new double[9];
							for (int i = 0; i < 9; i++) {
								c_ptr[i] = c_data[i];
							}

							camera_matrix = Mat(3, 3, CV_64F, c_ptr);
							dist_coeff = Mat::zeros(8, 1, CV_64F);
							first_frame = false;
							DebugInUnityMat<double>(camera_matrix);
						}
					}
				}
				else {
					DebugInUnity("Frame not valid");
				}
				m_locatableCameraFrame = locatableCameraFrame;
				//DebugInUnity("Release lock");
			}
			if (mat.data) {
				try {
					/*Mat bgr;
					cvtColor(mat, bgr, cv::COLOR_BGRA2BGR);
					Mat copy = bgr.clone();*/
					
					//DebugInUnityMat(v_matrix);
					aruco::detectMarkers(mat, dictionary, markerCorners, markerIds);
					if (markerIds.size() > 0) {
						//DebugInUnity("Recognize more 0 markers");
						//DebugInUnity(std::to_string(markerIds.at(0)));

						//Estimate pose MarkerfCamera translation and rotation vector;
						aruco::estimatePoseSingleMarkers(markerCorners, 0.054, camera_matrix, dist_coeff, rot_vecs, tranl_vecs);


						Vec3d rot_vec = rot_vecs.at(0);
						Vec3d tranl_vec = tranl_vecs.at(0);
						Mat rot_matrix = Mat(3, 3, CV_64F);
						cv::Rodrigues(rot_vec, rot_matrix);
						double cfm_data[] = { rot_matrix.at<double>(0,0), rot_matrix.at<double>(0,1), rot_matrix.at<double>(0,2), tranl_vec[0],
							rot_matrix.at<double>(1,0), rot_matrix.at<double>(1,1), rot_matrix.at<double>(1,2), tranl_vec[1],
							rot_matrix.at<double>(2,0), rot_matrix.at<double>(2,1), rot_matrix.at<double>(2,2), tranl_vec[2],
							0, 0, 0, 1 };
						Mat CamerafMarkerMat = Mat(4, 4, CV_64F, cfm_data);
						/*Mat CamerafMarker = MarkerfCamera.inv();*/
						//DebugInUnity("Camera from Marker");
						//DebugInUnityMat<double>(CamerafMarkerMat);
						if (RightToLeftMat.data) {
							CamerafMarkerMat = RightToLeftMat * CamerafMarkerMat * RightToLeftMat;
							//DebugInUnityMat<double>(CamerafMarkerMat);
						}
						HeadfOriginMat = HeadfCameraMat * CamerafMarkerMat;
						//callback to upload this matrix for Unity
						PassMatrix4x4(HeadfOriginMat, (int)MatrixType::HeadfOrigin);


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
					
				
			}else {
				DebugInUnity("frame is not valid");
			}
		}
	}

extern "C" __declspec(dllexport) void PassInMatrix(double* arr, int rows, int cols, int type) {
	double* mat_data = new double[rows*cols];
	std::copy(arr, arr + (rows*cols), mat_data);
	Mat mat(rows, cols, CV_64F, mat_data);
	if (type == (int)MatrixType::HeadfCamera) {
		HeadfCameraMat = mat;
		DebugInUnity("pass in headfcamera matrix");
		DebugInUnityMat<double>(HeadfCameraMat);

	}
	else if (type == (int)MatrixType::MarkerfOrigin) {
		MarkerfOriginMat = mat;
		DebugInUnity("pass in markerforigin matrix");
		DebugInUnityMat<double>(MarkerfOriginMat);
	}
	else if (type == (int)MatrixType::RightToLeft) {
		RightToLeftMat = mat;
	}
}
