#pragma once
#include "pch.h"
#include <SimpleMath.h>
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>

using namespace cv;
namespace HololensCamera {
	class LocatableCameraFrame {
	public:
		LocatableCameraFrame(
			unsigned int id,
			Windows::Graphics::Imaging::SoftwareBitmap^ softwareBitmap,
			Windows::Perception::Spatial::SpatialCoordinateSystem^ coordinateSystem,
			Mat viewTransform,
			Mat projectionTransform);
		~LocatableCameraFrame();
		unsigned int GetId() { return m_id; }
		Windows::Graphics::Imaging::SoftwareBitmap^ GetSoftwareBitmap() { return m_softwareBitmap; }
		Windows::Perception::Spatial::SpatialCoordinateSystem^ GetCoordinateSystem() { return m_coordinateSystem; }

		/*DirectX::SimpleMath::Matrix GetViewTransform() { return m_viewTransform; }
		DirectX::SimpleMath::Matrix GetProjectionTransform() { return m_projectionTransform; }*/
		Mat GetViewTransform(){ return m_viewTransform; }
		Mat GetProjectionTransform() { return m_projectionTransform; }

	private:
		unsigned int m_id;
		Windows::Graphics::Imaging::SoftwareBitmap^ m_softwareBitmap;
		Windows::Perception::Spatial::SpatialCoordinateSystem^ m_coordinateSystem;
		/*DirectX::SimpleMath::Matrix m_viewTransform;
		DirectX::SimpleMath::Matrix m_projectionTransform;*/
		Mat m_viewTransform;
		Mat m_projectionTransform;
		/*Mat m_worldfCameraQuat;
		Mat m_worldfCameraPos;*/

	};

}
