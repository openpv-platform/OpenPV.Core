using Ahsoka.Storyboard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class StoryboardTests
{
    readonly Queue<TestSBIOObject> resultObjects = new();
    readonly Semaphore resultSem = new(0, 100);
    readonly object syncRoot = new();

    [TestMethod]
    public void StoryboardTest()
    {
        // Create Mock API for Testing
        StoryboardClient runtime = new(api: new StoryboardAPIMock());
        runtime.MessageReceived += Runtime_MessageReceived;
        runtime.StartEventListenerAsync();

        // Send First Object...It should be received above
        TestSBIOObject myObject = CreateTestObject();
        runtime.SendEvent(myObject);

        // Wait for Messages to Send.
        TestSBIOObject resultObject = null;
        resultSem.WaitOne();
        lock (syncRoot)
            resultObject = resultObjects.Dequeue();

        // Float Values
        Assert.AreEqual(myObject.ValueFloat, resultObject.ValueFloat, "Float Values did Not Match for Object1");

        // Compare SignedInt Values
        Assert.AreEqual(myObject.ValueInt64, resultObject?.ValueInt64, "Int64 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueInt32, resultObject?.ValueInt32, "Int32 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueInt16, resultObject?.ValueInt16, "Int16 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueInt8, resultObject?.ValueInt8, "Int8 Values did Not Match for Object1");

        // Compare Unsigned Int Values
        Assert.AreEqual(myObject.ValueUInt64, resultObject?.ValueUInt64, "UInt64 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueUInt32, resultObject?.ValueUInt32, "UInt32 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueUInt16, resultObject?.ValueUInt16, "UInt16 Values did Not Match for Object1");
        Assert.AreEqual(myObject.ValueUInt8, resultObject?.ValueUInt8, "UInt8 Values did Not Match for Object1");

        // Compare String
        Assert.AreEqual(myObject.ValueString, resultObject?.ValueString, "String Values did Not Match for Object1");

        // Compare Array Value
        CollectionAssert.AreEqual(myObject.ValueIntArray, resultObject?.ValueIntArray, "Int[] Values did Not Match for Object1");

        // Run Test on Storyboard Interface
        RunPerformanceTest(runtime, 10000);

        // Finish test
        runtime.StopEventListener();
    }

    private void RunPerformanceTest(StoryboardClient runtime, int count)
    {
        // Run Perf Test.
        Stopwatch watch = new();
        watch.Start();

        for (int i = 0; i < count; i++)
        {
            // Send First Object...It should be received above
            TestSBIOObject myObject = CreateTestObject();
            runtime.SendEvent(myObject);

            // Wait for Messages to Send.
            resultSem.WaitOne();
            lock (syncRoot)
                resultObjects.Dequeue();
        }
        watch.Stop();

        Console.WriteLine("");
        Console.WriteLine($"Performance Test: {count} Calls completed in {watch.ElapsedMilliseconds}ms");
        Console.WriteLine($"                : {((float)count / watch.ElapsedMilliseconds) * 1000} Calls Per Second");
        Console.WriteLine("");
    }

    private void Runtime_MessageReceived(object sender, StoryboardEventArgs e)
    {
        // Decode Object
        TestSBIOObject myObject = e.Message.GetObject<TestSBIOObject>();

        lock (syncRoot)
            resultObjects.Enqueue(myObject);

        resultSem.Release();
    }

    private static TestSBIOObject CreateTestObject()
    {
        TestSBIOObject myObject1 = new()
        {
            ValueFloat = 100.4f,
            ValueInt64 = -64,
            ValueInt32 = -32,
            ValueInt16 = -16,
            ValueInt8 = -8,
            ValueUInt64 = 64,
            ValueUInt32 = 32,
            ValueUInt16 = 16,
            ValueUInt8 = 8,

            ValueString = "JakeIsCodingForFun",
            ValueIntArray = new int[5] { 1, 2, 3, 4, 5 }
        };

        return myObject1;
    }

    [TestMethod]
    public void ClassGenerationTest()
    {
        //folder the test class is written in
        string pathToDestination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        //path to event file
        string pathToSource = Path.GetTempFileName();
        File.WriteAllText(pathToSource, Properties.Resources.EventFile);
        //namespace to be used for the generated classes
        string nameSpace = "Ahsoka.Test";

        CommandLine.Execute(new string[] { "--GenerateStoryboardClasses", pathToSource, pathToDestination, nameSpace, "StoryboardViewModelBase" });
        //StoryboardTools.GenerateMessageClasses(pathToSource, pathToDestination, nameSpace, "StoryboardViewModelBase");

        string value = File.ReadAllText(Path.Combine(pathToDestination, "GenerateEventTest.cs"));
        Assert.IsTrue(value.Contains("using Ahsoka.Storyboard"));
        Directory.Delete(pathToDestination, true);
        File.Delete(pathToSource);
    }
}

[SBMessageEventName()]
public class TestSBIOObject
{
    [SBField(1)]
    public float ValueFloat { get; set; }

    [SBField(2)]
    public Int64 ValueInt64 { get; set; }

    [SBField(3)]
    public Int32 ValueInt32 { get; set; }

    [SBField(4)]
    public Int16 ValueInt16 { get; set; }

    [SBField(5)]
    public sbyte ValueInt8 { get; set; }

    [SBField(6)]
    public UInt64 ValueUInt64 { get; set; }

    [SBField(7)]
    public UInt32 ValueUInt32 { get; set; }

    [SBField(8)]
    public UInt16 ValueUInt16 { get; set; }

    [SBField(9)]
    public byte ValueUInt8 { get; set; }

    [SBField(10, 5)]
    public int[] ValueIntArray { get; set; } = new int[5];

    [SBField(11)]
    public string ValueString { get; set; }
}
