#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaConfiguration.h"
#include "AhsokaServiceEndPoint.h"
#include "LayerManagerServiceMessages.h"
#include "ReadyWaiter.h"
#include "LayerManagerService.h"

#include <pthread.h>
#include "ilm/ilm_control.h"
#include <ilm/ivi-application-client-protocol.h>


using namespace std;
using namespace AhsokaCore;
using namespace AhsokaServiceFramework;

namespace AhsokaLayerManager
{
	class LayerManagerService : public AhsokaServiceFramework::AhsokaServiceEndPoint<LayerManagerMessageTypes_Ids>
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

			static void LayerCallback(ilmObjectType object, t_ilm_uint id, t_ilm_bool created, void *user_data);
			static void ConfigureSurface(t_ilm_uint id, t_ilm_uint width, t_ilm_uint height);
			static void SurfaceCallback(t_ilm_uint id, struct ilmSurfaceProperties* sp, t_ilm_notification_mask m);
			static void WaylandShutdown(t_ilm_shutdown_error_type error_type, int errornum, void *user_data);
	
			// Surface Config Functions
			static SurfaceSetup GetSurfaceSetup(int surfaceId);
            static void UpdateSurfaceSetup(SurfaceSetup& newSetup);
            static bool IsSurfaceActive(int surfaceId);
   			static void ValidateSurface(SurfaceSetup& setup, int id, int aRequestedWidth, int aRequestedHeight);

			typedef struct 
			{
				// We Default to Layer 1000 and Screen 0
			    t_ilm_uint layerId;
				t_ilm_uint screenId;
				t_ilm_uint screenWidth;
				t_ilm_uint screenHeight;

				ReadyWaiter waitForExit;
				std::vector<t_ilm_uint> surfaceList;

				AhsokaLayerManager::LayerManagerConfiguration layerConfiguration;

			} LayerManagerContext;


			static LayerManagerContext context;
			
	};
}