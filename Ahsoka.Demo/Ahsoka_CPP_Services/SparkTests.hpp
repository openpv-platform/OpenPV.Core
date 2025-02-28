#pragma once

#include <iostream>
#include <string>
#include "AhsokaServices.h"
#include "AhsokaRuntimeProcess.h"

class SparkTests
{
    public:
        void RunTests(std::string pathToServiceExe)
        {
            auto serviceProcess = AhsokaRuntimeProcess::StartServiceExecutable(pathToServiceExe);

            SystemServiceClient client;
            client.Start();

            SparkServiceClient sparkClient;
            sparkClient.Start();

            std::cout << client.Echo("Server Ready") << ::endl;

            SparkSubscription newSub;
            newSub.set_release_name("Test Release");
            newSub.set_channel_name("Test Release00000000-0000-0000-0000-000000000000");
            newSub.set_subscription_url("enovation.powervisionspark.com");

            sparkClient.SetSparkSubscription(newSub);

            auto resultSub = sparkClient.GetSparkSubscription();

            if (newSub.release_name() == resultSub.get()->release_name() && newSub.channel_name() == resultSub.get()->channel_name() 
                && newSub.subscription_url() == resultSub.get()->subscription_url())
            {
                std::cout << "Subscription set successfully" << std::endl;
            }
            else std::cout << "Subscription set failed" << std::endl;

            client.RequestServiceShutdown();
            
            client.Stop();
            sparkClient.Stop();

            serviceProcess->EndProcess();

            std::cout << "Shutdown Completed" << std::endl;
        }
};