
std::string SystemServiceClient::Echo(std::string value)
{
	AhsokaSystem::EchoValue request;
    request.set_data_value(value);
    auto resultObject = SendMessageWithResponse<AhsokaSystem::EchoValue>(SystemMessageTypes_Ids_ECHO, &request);
    return resultObject->data_value();
}

void SystemServiceClient::RequestServiceShutdown(ShutdownTypes type, int exitCode)
{
    AhsokaSystem::ShutdownInformation request;
    request.set_shutdown_type(type);
    request.set_shutdown_code(exitCode);
    SendMessageWithResponse(SystemMessageTypes_Ids_REQUEST_SHUTDOWN, &request);
}
