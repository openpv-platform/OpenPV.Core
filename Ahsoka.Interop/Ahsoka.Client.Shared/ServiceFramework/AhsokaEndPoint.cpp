#include "IAhsokaEndPoint.h"
#include "AhsokaEndPoint.h"

namespace AhsokaServiceFramework
{

	AhsokaEndPoint::~AhsokaEndPoint() = default;

    AhsokaEndPoint::AhsokaEndPoint(IAhsokaEndPoint* endPoint)
        :EndPoint(endPoint)
    {
        
    }

    IAhsokaEndPoint* AhsokaEndPoint::GetEndPoint()
    {
        return EndPoint.get();
    }

    
}