#pragma once

// Framework
#include "AhsokaRuntimeBuilder.h"
#include "AhsokaConfiguration.h"

// Messages and Clients
#include "Services.pb.h"

#include "SystemService.pb.h"
#include "SystemServiceClient.h"

#include "SparkService.pb.h"
#include "SparkServiceClient.h"

#include "IotService.pb.h"
#include "IotServiceClient.h"

#include "NetworkService.pb.h"
#include "NetworkServiceClient.h"

#include "CanService.pb.h"
#include "CanConfiguration.pb.h"
#include "CanServiceClient.h"

#include "BLEService.pb.h"
#include "BLEServiceClient.h"

#include "IOService.pb.h"
#include "IOServiceClient.h"

#include "InstallService.pb.h"
#include "InstallServiceClient.h"

#include "DataService.pb.h"
#include "DataServiceClient.h"

using namespace AhsokaSystem;
using namespace AhsokaSpark;
using namespace AhsokaServiceFramework;
using namespace AhsokaIot;
using namespace AhsokaNetwork;
using namespace AhsokaCAN;
using namespace AhsokaIO;
using namespace AhsokaBLE;
using namespace AhsokaInstall;