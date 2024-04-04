
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahsoka.Test;

[TestClass]
public class TestInitializer
{
   [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        AhsokaExtensions.LoadExtensions();
    }

}
