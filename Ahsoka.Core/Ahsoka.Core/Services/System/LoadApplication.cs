using Ahsoka.Core.Utility;
using System;

namespace Ahsoka.Services.System;

/// <summary>
/// Message used for loading an application.
/// </summary>
public partial class LoadApplication : IFileTransmit
{

}

/// <summary>
/// Message Response for Load Applications
/// </summary>
public partial class LoadApplicationReply : IFileTransmitReply
{

}

/// <summary>
/// Message for Carrying Data for an Application Load.
/// </summary>
public partial class LoadApplicationData : IFileTransmitData
{

}

/// <summary>
/// Message Reply for Application Data Messages
/// </summary>
public partial class LoadApplicationDataReply : IFileTransmitDataReply
{

}

/// <summary>
///  Message sent to Cancel an Application Load
/// </summary>
public partial class LoadApplicationCancelled : IFileTransmitCancelled
{

}

/// <summary>
/// Event Args raised when an Application Load is Cancelled.
/// </summary>
public partial class LoadApplicationCompleteEventArgs : EventArgs
{

}