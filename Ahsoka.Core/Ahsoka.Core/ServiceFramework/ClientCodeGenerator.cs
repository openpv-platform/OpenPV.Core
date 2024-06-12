#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.Spark;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ahsoka.ServiceFramework;

public class ClientCodeGenerator
{
    static string includes = "";
    static string usings = "";
    public static void GenerateClients(PackageInformation packageInfo, string pathToOutputFolder)
    {
        // Create Output Directory
        if (!Directory.Exists(pathToOutputFolder)) { Directory.CreateDirectory(pathToOutputFolder); }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"");
        includes = "#pragma once\n#include \"AhsokaRuntimeBuilder.h\"\n#include \"AhsokaConfiguration.h\"\n#include \"Services.pb.h\"\n";
        usings = "using namespace AhsokaServiceFramework;\n";

        foreach (var item in ClassLoader.GetItemsWithInterface(typeof(AhsokaMessagesBase)))
        {
            if (packageInfo.ServiceInfo.RuntimeConfiguration.Services.Any(x => item.FullName.Contains(x.ServiceName)))
            {
                string name = item.Name.Replace("Messages", "");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Found Client / Messages to Generate Named {name}");
                GenerateClientsForCPP(name, ProcessNameSpace(name), pathToOutputFolder, item);
            }    
        }

        File.WriteAllText(Path.Combine(pathToOutputFolder, "inc", "services", "AhsokaServices.h"), includes + usings);
    }

    private static void GenerateClientsForCPP(string name, string nameSpace, string pathToOutput, Type messageType)
    {
        GenerateMessages(name, nameSpace, pathToOutput, messageType);

        GenerateClientHeaders(name, nameSpace, pathToOutput, messageType);
        GenerateClientImplementations(name, nameSpace, pathToOutput, messageType);

        GenerateAdditionalResources(name,  pathToOutput, nameSpace, messageType);
    }

    private static void GenerateAdditionalResources(string name, string pathToOutput, string nameSpace, Type messageType)
    {
        var messages = ClassLoader.CreateType(messageType) as AhsokaMessagesBase;

        foreach (var file in messages.GetAdditionalClientResources(ApplicationType.Cpp)) 
        {
            string path = Path.Combine(pathToOutput, file.Key);
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, file.Value);
        }

        includes += messages.GetInteropIncludes(name, ApplicationType.Cpp);

        usings += $"using namespace {nameSpace};\n";
    }

    private static void GenerateClientHeaders(string name, string nameSpace, string pathToOutput, Type messageType)
    {
        var messages = ClassLoader.CreateType(messageType) as AhsokaMessagesBase;

        var headerBuilder = new StringBuilder();
        var notificationBuilder = new StringBuilder();

        const string messageMacro = "\t\t\t{0} {1}({2}& {3}, int timeoutInMs = INT_MAX);\r\n";
        const string messageMacroVoid = "\t\t\t{0} {1}(int timeoutInMs = INT_MAX);\r\n";

        foreach (var item in messages.Info)
        {
            if (item.Value.HideInGeneratedClients || !item.Value.AutoGenerateImplementation)
                continue;

            var request = ProcessArgType(nameSpace, item.Value.RequestType, false);
            var requestName = ProcessArgName(item.Value.RequestType);
            var reply = ProcessArgType(nameSpace, item.Value.ReplyType, true);

            if (item.Value.IsNotification)
            {
                string transportId = ProcessTransportID(item.Value.TransportId, item.Value.TransportId.GetType());
                notificationBuilder.AppendLine($"\t\t\t// TransportID: {transportId} MessageType: {reply}");
                continue;
            }

            if (request == "void")
                headerBuilder.AppendFormat(messageMacroVoid, reply, item.Value.TransportId.ToString());
            else
                headerBuilder.AppendFormat(messageMacro, reply, item.Value.TransportId.ToString(), request, requestName);
        }

        // Write Messages
        string fileToWrite = Properties.Resources.CPPClientHeader
            .Replace("DefaultNameSpace", nameSpace)
            .Replace("DefaultService", name)
            .Replace("DefaultTransportIdEnum", name.Replace("Service", "") + "MessageTypes_Ids")
            .Replace("%SERVICE_METHODS%", headerBuilder.ToString())
            .Replace("%SERVICE_NOTIFICATIONS%", notificationBuilder.ToString());

        string pathToFile = Path.Combine(pathToOutput, "inc", "services", $"{name}Client.h");
        File.WriteAllText(pathToFile, fileToWrite.ReplaceLineEndings("\n"));
    }

    private static void GenerateClientImplementations(string name, string nameSpace, string pathToOutput, Type messageType)
    {
        var messages = ClassLoader.CreateType(messageType) as AhsokaMessagesBase;

        var methodBuilder = new StringBuilder();
        var notificationBuilder = new StringBuilder();

        const string messageMacro = "\t{0} {1}Client::{2}({3}& {4}, int timeoutInMs)\r\n";
        const string messageMacroVoid = "\t{0} {1}Client::{2}(int timeoutInMs)\r\n";
        const string startEnd = "\t{0}\r\n";

        const string bodyMacro = "\t\treturn GetEndPoint<{0}_t>()->SendNotificationWithResponse<{1}>({2});\r\n";
        const string bodyMacroNoReturn = "\t\tGetEndPoint<{0}_t>()->SendNotificationWithResponse({1});\r\n";

        foreach (var item in messages.Info)
        {
            if (item.Value.HideInGeneratedClients || !item.Value.AutoGenerateImplementation)
                continue;

            var request = ProcessArgType(nameSpace, item.Value.RequestType, false);
            var requestName = ProcessArgName(item.Value.RequestType);
            var reply = ProcessArgType(nameSpace, item.Value.ReplyType, true);

            string transportId = ProcessTransportID(item.Value.TransportId, item.Value.TransportId.GetType());

            if (item.Value.IsNotification)
            {
                notificationBuilder.AppendLine($"\t\t\t// TransportID: {transportId} MessageType: {reply}");
                continue;
            }

            // Generate Header
            string arguments;

            if (request == "void")
            {
                arguments = $"{transportId}, 0, timeoutInMs";
                methodBuilder.AppendFormat(messageMacroVoid, reply, name, item.Value.TransportId.ToString());
            }
            else
            {
                arguments = $"{transportId}, &{request.Substring(0, 1).ToLower()}{request.Substring(1)}, timeoutInMs";
                methodBuilder.AppendFormat(messageMacro, reply, name, item.Value.TransportId.ToString(), request, requestName);
            }
            methodBuilder.AppendFormat(startEnd, "{");



            if (reply == "void")
                methodBuilder.AppendFormat(bodyMacroNoReturn, name, arguments);
            else
                methodBuilder.AppendFormat(bodyMacro, name, reply.Replace("unique_ptr<", "").Replace(">", ""), arguments);


            methodBuilder.AppendFormat(startEnd, "}\r\n");
        }

        // Write Messages
        string fileToWrite = Properties.Resources.CPPClientCode
            .Replace("DefaultNameSpace", nameSpace)
            .Replace("DefaultService", name)
            .Replace("DefaultTransportIdEnum", name.Replace("Service", "") + "MessageTypes_Ids")
            .Replace("%SERVICE_METHODS%", methodBuilder.ToString())
            .Replace("%SERVICE_NOTIFICATIONS%", notificationBuilder.ToString());

        string pathToFileCPP = Path.Combine(pathToOutput, "inc", "services", $"{name}Client.cpp");
        File.WriteAllText(pathToFileCPP, fileToWrite.ToString());
    }

    private static void GenerateMessages(string name, string nameSpace, string pathToOutput, Type messageType)
    {
        var messages = ClassLoader.CreateType(messageType) as AhsokaMessagesBase;

        var messageItems = new StringBuilder();
        const string messageMacro = "\t\t\t\tRegisterServiceRequest({0}, CreateCTOR<{1}>(), CreateCTOR<{2}>());\r\n";
        const string notifMacro = "\t\t\t\tRegisterServiceNotification({0}, CreateCTOR<{1}>());\r\n";

        foreach (var item in messages.Info)
        {
            if (item.Value.HideInGeneratedClients)
                continue;

            string transportId = ProcessTransportID(item.Value.TransportId, item.Value.TransportId.GetType());
            var request = ProcessType(nameSpace, item.Value.RequestType);
            var reply = ProcessType(nameSpace, item.Value.ReplyType);

            if (item.Value.IsNotification)
                messageItems.AppendFormat(notifMacro, transportId, request);
            else
                messageItems.AppendFormat(messageMacro, transportId, request, reply);
        }

        // Write Messages
        string fileToWrite = Properties.Resources.CPPClientMessages
            .Replace("DefaultNameSpace", nameSpace)
            .Replace("DefaultServiceMessages", name + "Messages")
            .Replace("%REGISTER_SERVICE_MESSAGES%", messageItems.ToString());

        string pathToFile = Path.Combine(pathToOutput, "inc","services", $"{name}Messages.hpp");
        File.WriteAllText(pathToFile, fileToWrite.ReplaceLineEndings("\n"));
    }

    private static string ProcessType(string nameSpace, Type type)
    {
        if (type == typeof(EmptyNotification))
            return "AhsokaServiceFramework::EmptyNotification";

        return $"{nameSpace}::{type.Name}";
    }

    private static string ProcessArgName(Type type)
    {
        if (type == typeof(EmptyNotification))
            return "void";

        return $"{type.Name.Substring(0, 1).ToLower()}{type.Name.Substring(1)}";
    }

    private static string ProcessArgType(string nameSpace, Type type, bool isReturn)
    {
        if (type == typeof(EmptyNotification))
            return "void";

        if (isReturn)
            return $"unique_ptr<{type.Name}>";
        else
            return $"{type.Name}";
    }

    private static string ProcessTransportID(Enum transportId, Type type)
    {
        string enumString = Regex.Replace(transportId.ToString(), "(?<=.)([A-Z])", "_$0", RegexOptions.Compiled).ToUpperInvariant();
        string fullName = type.FullName.Replace("+", "_");
        string nameWithoutNameSpace = fullName.Replace(type.Namespace + ".", "");
        return nameWithoutNameSpace + "_" + enumString;
    }

    private static string ProcessNameSpace(string name)
    {
        return "Ahsoka" + name.Replace("Service", "")
                    .Replace("Can", "CAN");
    }
}