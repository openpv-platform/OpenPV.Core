using ProtoBuf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Install;

[ProtoContract]
public class InstallMessageTypes
{
    public enum Ids
    {
        None = 0,
        InstallPackage = 1,
        InstallProgress = 2,
        InstallLog = 4,
    }
}

[ProtoContract]
public class InstallPackageRequest
{
    [ProtoMember(1)]
    public string InstallPackagePath { get; set; }

    [ProtoMember(2)]
    public string InstallPassKey { get; set; }
}

[ProtoContract]
public class PackageProgressInfo
{
    [ProtoMember(1)]
    public string Message { get; set; }
}

[ProtoContract]
public class PackageCompleteInfo
{
    [ProtoMember(1)]
    public string Message { get; set; }
}

[ProtoContract]
public class InstallProgressInfo
{
    [ProtoMember(1)]
    public int PercentComplete { get; set; }

    [ProtoMember(2)]
    public string Message { get; set; }

    [ProtoMember(3)]
    public bool UpdatePercentOnly { get; set; }
}

[ProtoContract]
public class InstallCompleteInfo
{
    [ProtoMember(1)]
    public InstallResult Result { get; set; }

    [ProtoMember(2)]
    public string Message { get; set; }
}

[ProtoContract]
public class InstallLogInfo
{
    [ProtoMember(1)]
    public LogMessageType LogMessageType { get; set; }

    [ProtoMember(2)]
    public string LogMessage { get; set; }
}

public enum InstallResult
{
    Success = 0,
    UnknownError = 1,
    PreviousInstallInProgress = 2,
    ExceptionThrownDuringInstall = 3,
    InvalidFile = 301,                       	//-   301 - Invalid file: Can't Read
    PartNumgerMatchFailed = 303,                //-   303 - Part Number Match failed
    InvalidComponentFile = 304,		    		//-   304 - Corrupted Component File (CRC Mismatch)
    InvalidComponentType = 307,              	//-   307 - Unknown Component Type
    PassKeyIncorrect = 309,                  	//-   309 - PassKey incorrect
    UnknownHardwareFamily = 402,			 	//-   402 - Unknown hardware family
    MissingPartNumber = 403,                 	//-   403 - Missing part number
    MissingSerialNumber = 404,                  //-   404 - Missing serial number*/
    MissingLicenseForComponent = 405,           //-   405 - A Software License could not be found for the Application Type / Module.
}

public enum LogMessageType
{
    Error = 0,
    Warning = 1,
    Information = 2,
}
