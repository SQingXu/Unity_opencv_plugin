#include <opencv2/aruco.hpp>
#include <vector>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <stdio.h>
#include <opencv2/opencv.hpp>

using namespace cv;
int main()
{
	Mat marker;
	Ptr<aruco::Dictionary> dictionary = aruco::getPredefinedDictionary(aruco::DICT_6X6_250);
	aruco::drawMarker(dictionary, 23, 200, marker, 1);
	imshow("Marker", marker);
	imwrite("test23.jpg", marker);
	waitKey(0);

	return 0;
}
