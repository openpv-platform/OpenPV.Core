#pragma warning disable CS1591
using Ahsoka.Core;
using System;

namespace Ahsoka.Installer.InstallEngine;

[AttributeUsage(AttributeTargets.Class)]
public class PlatformFamilyAttribute : Attribute
{
    public PlatformFamily PlatformFamily { get; internal set; }

    public string PlatformQualifier { get; internal set; }

    public PlatformFamilyAttribute(PlatformFamily platformFamily, string platformQualifier)
    {
        PlatformFamily = platformFamily;
        PlatformQualifier = platformQualifier;
    }
}
