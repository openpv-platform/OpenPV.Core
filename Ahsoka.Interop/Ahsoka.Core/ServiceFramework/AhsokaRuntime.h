#pragma once
#include <memory>
#include <string>
#include <vector>

#include "IAhsokaEndPoint.h"

namespace AhsokaServiceFramework
{
	
	class AhsokaRuntime
	{
		public:

			static AhsokaRuntime& GetDefaultRuntime();
			static void ReleaseStartup();
			
		    void StartEndPoints(std::vector<IAhsokaEndPoint*>);
		    void StopEndPoints(std::vector<IAhsokaEndPoint*>);
			void StopAllEndPoints();
			
			void AddEndPoint(IAhsokaEndPoint*);
			void RemoveEndPoint(IAhsokaEndPoint* endPoint);
			
			~AhsokaRuntime() = default;
	
		private:

			AhsokaRuntime() = default;
		    std::map<std::string, IAhsokaEndPoint*> endPoints;
			
	};
}