using Ahsoka.Core;
using Ahsoka.Core.Utility;

namespace Ahsoka.Services.System;

/// <summary>
/// Get Hardware Information
/// </summary>
public partial class GetHardwareInformationReply
{
    /// <summary>
    /// Platform As Enumeration
    /// </summary>
    public PlatformFamily PlatformFamilyEnum
    {
        get { return this.PlatformFamily.IntToEnum<PlatformFamily>(); }
        set { this.PlatformFamily = value.EnumToInt(); }
    }
}