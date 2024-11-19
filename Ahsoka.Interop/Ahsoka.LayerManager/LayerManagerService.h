#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"
#include "AhsokaServiceEndPoint.h"
#include "LayerManagerServiceMessages.h"
#include "ReadyWaiter.h"
#include "LayerManagerConfigReader.h"
#include "LayerManagerService.h"

#include <pthread.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>


using namespace std;
using namespace AhsokaSystem;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{
	class LayerManagerService : public AhsokaServiceFramework::AhsokaServiceEndPoint<LayoutManagerMessageTypes_Ids>
	{
		public:

			LayerManagerService();
			void OnHandleReceive(AhsokaMessageHeader& messageHeader, AhsokaMessageType_t& message);
			void StartAndRun();
		
		private:
			
			int Init();
			void CreateScreen();
			void StartLayoutService();
			
			ServiceConfiguration localConfig; 
			
			static void RequestShutdown(int);

			static void LayerManagerCallback(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data);
			static void ConfigureSurface(t_ilm_uint id, t_ilm_uint width, t_ilm_uint height);
			static void SurfaceCallback(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m);
			static void WaylandShutdown(t_ilm_shutdown_error_type error_type, int errornum, void *user_data);
	
			// We Default to Layer 1000 and Screen 0
			static t_ilm_uint layer;
			static t_ilm_uint screen_ID;
			static t_ilm_uint screenWidth;
			static t_ilm_uint screenHeight;
			static int number_of_surfaces;
			static bool showUnknownSurfaces;

			static pthread_mutex_t mutex;
			static pthread_cond_t  waiterVariable;

			static ReadyWaiter waitForExit;
			static std::vector<int> surfaceList;
	};
}