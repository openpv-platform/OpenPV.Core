using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

internal static class SparkTestsHelpers
{
    internal static void GetSparkLogin(out string identity, out string password, out string url)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
          .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
          .Build();

        try
        {
            url = configuration.GetSection("SparkUrl").Value;
            identity = configuration.GetSection("SparkLogin").Value;
            password = configuration.GetSection("SparkPassword").Value;
        }
        catch
        {
            identity = password = url = null;

            Console.WriteLine("Could not find User Secrets for 'SparkLogin' and 'SparkPassword' needed for this test");
            Console.WriteLine("To add them, right click on the Ahsoka.Test Project and Select Manage User Secrets");
            Console.WriteLine("In order to test Spark, the user must have User and Provisioning Rights to SparkServiceDev");
            Assert.Inconclusive("Test was not able to access User Secrets");
        }
    }
}