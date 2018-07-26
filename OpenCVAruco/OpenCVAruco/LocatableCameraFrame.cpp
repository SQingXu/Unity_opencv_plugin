#include "LocatableCameraFrame.h"

namespace HololensCamera {
	LocatableCameraFrame::LocatableCameraFrame(
		unsigned int id,
		Windows::Graphics::Imaging::SoftwareBitmap^ softwareBitmap,
		Windows::Perception::Spatial::SpatialCoordinateSystem^ coordinateSystem,
		cv::Mat viewTransform,
		cv::Mat projectionTransform)
		: m_id(id)
		, m_softwareBitmap(softwareBitmap)
		, m_coordinateSystem(coordinateSystem)
		, m_viewTransform(viewTransform)
		, m_projectionTransform(projectionTransform)
	{
	}
	LocatableCameraFrame::~LocatableCameraFrame() {

	}
}