#pragma warning disable CS1591
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using System;
using System.Collections.Generic;

namespace Ahsoka.Spark;

public class SparkConnectionInfo
{
    public string Url { get; set; }

    public AuthorizationDTO CurrentAuthorization { get; set; }

    public string Login { get; set; }

    public string Password { get; set; }

    internal void LoginToTeamClient()
    {
        if (SparkTeamClient.SharedSparkClient.CurrentAuthorization == null)
            if (this.CurrentAuthorization != null)
                SparkTeamClient.SharedSparkClient.LoginWithRefreshToken(Url, this.CurrentAuthorization);
            else
                CurrentAuthorization = SparkTeamClient.SharedSparkClient.LoginToSpark(Url, SparkEndPointType.Teams, SecurityGroopType.Account, Login, SecureLocalStrings.ToSecureString(Password), null);
    }

    internal void LoginToProvisioningClient()
    {
        if (SparkProvisioningClient.SharedSparkClient.CurrentAuthorization == null)
            if (this.CurrentAuthorization != null)
                SparkProvisioningClient.SharedSparkClient.LoginWithRefreshToken(Url, this.CurrentAuthorization);
            else
                CurrentAuthorization = SparkProvisioningClient.SharedSparkClient.LoginToSpark(Url, SparkEndPointType.Provisioning, SecurityGroopType.Provisioning, Login, SecureLocalStrings.ToSecureString(Password), null);
    }
}


public interface IProvisionDisplayRequest
{
    string PackageName { get; }
    Guid ApplicationId { get; }
    List<ModuleLicense> Licenses { get; }
}

public class ProvisionDisplayRequest : IProvisionDisplayRequest
{
    public string PackageName { get; set; }
    public Guid ApplicationId { get; set; }
    public List<ModuleLicense> Licenses { get; init; } = new();
}

