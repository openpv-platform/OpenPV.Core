using Ahsoka.Storyboard;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class StoryboardTests : LinearTestBase
{
    readonly Queue<StoryboardViewModelBase> resultObjects = new();
    readonly Semaphore resultSem = new(0, 100);
    readonly object syncRoot = new();

    [TestMethod]
    public void StoryboardTest()
    {
        // Create Mock API for Testing
        StoryboardClient runtime = new(metadata: StoryboardMetadata.EventMetadata, api: new StoryboardAPIMock());
        runtime.MessageReceived += Runtime_MessageReceived;
        runtime.StartEventListenerAsync();

        // Send First Object...It should be received above
        TestSbioObject myObject = CreateTestObject();
        runtime.SendEvent(myObject.Message);

        // Wait for Messages to Send.
        TestSbioObject resultObject = null;
        resultSem.WaitOne();
        lock (syncRoot)
            resultObject = (TestSbioObject)resultObjects.Dequeue();

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
        Assert.IsFalse(myObject.ValueString.IsNullOrEmpty(), "String Value was not set correctly");
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
            TestSbioObject myObject = CreateTestObject();
            runtime.SendEvent(myObject.Message);

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
        var myObject = e.Message.GetObject<StoryboardViewModelBase>();

        lock (syncRoot)
            resultObjects.Enqueue(myObject);

        resultSem.Release();
    }

    private static TestSbioObject CreateTestObject()
    {
        TestSbioObject myObject1 = new()
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

        //CommandLine.Execute(new string[] { "--GenerateStoryboardClasses", pathToSource, pathToDestination, nameSpace, "StoryboardViewModelBase" });
        StoryboardTools.GenerateMessageClasses(pathToSource, pathToDestination, nameSpace, "StoryboardViewModelBase");

        string value = File.ReadAllText(Path.Combine(pathToDestination, "GeneratedStoryboardObjects.cs"));
        Assert.IsTrue(value.Contains("using Ahsoka.Storyboard"));
        Directory.Delete(pathToDestination, true);
        File.Delete(pathToSource);
    }
}

public class TestSbioObject : StoryboardViewModelBase
{
    public const string Name = "test_sbio_object";
    public const string Format = "";
    public const StoryboardMessageDirection Direction = StoryboardMessageDirection.In;

    public float ValueFloat
    {
        get { return OnGetValue<float>(); }
        set { OnSetValue(value); }
    }

    public Int64 ValueInt64
    {
        get { return OnGetValue<Int64>(); }
        set { OnSetValue(value); }
    }

    public Int32 ValueInt32
    {
        get { return OnGetValue<Int32>(); }
        set { OnSetValue(value); }
    }

    public Int16 ValueInt16
    {
        get { return OnGetValue<Int16>(); }
        set { OnSetValue(value); }
    }

    public sbyte ValueInt8
    {
        get { return OnGetValue<sbyte>(); }
        set { OnSetValue(value); }
    }

    public UInt64 ValueUInt64
    {
        get { return OnGetValue<UInt64>(); }
        set { OnSetValue(value); }
    }

    public UInt32 ValueUInt32
    {
        get { return OnGetValue<UInt32>(); }
        set { OnSetValue(value); }
    }

    public UInt16 ValueUInt16
    {
        get { return OnGetValue<UInt16>(); }
        set { OnSetValue(value); }
    }

    public byte ValueUInt8
    {
        get { return OnGetValue<byte>(); }
        set { OnSetValue(value); }
    }

    public int[] ValueIntArray
    {
        get { return OnGetValue<int[]>(); }
        set { OnSetValue(value); }
    }

    public string ValueString
    {
        get { return OnGetValue<string>(); }
        set { OnSetValue(value); }
    }

    public override Dictionary<string, StoryboardPropertyInfo> GetMetadata() { return StoryboardMetadata.EventMetadata.GetMetadata(Name); }
    public TestSbioObject() : base(Name, Format, Direction) { }
    public TestSbioObject(StoryboardMessage message) : base(message) { }
}

public class StoryboardMetadata : IStoryboardMetadata
{
    Dictionary<string, Dictionary<string, StoryboardPropertyInfo>> metadata = new();

    static StoryboardMetadata eventMetadata = null;

    public Dictionary<string, Dictionary<string, StoryboardPropertyInfo>> GetMetadata() { return metadata; }

    public Dictionary<string, StoryboardPropertyInfo> GetMetadata(string eventName) { return metadata[eventName]; }

    public void AddMetadata(string eventName, Dictionary<string, StoryboardPropertyInfo> properties)
    {
        metadata.Add(eventName, properties);
    }

    public StoryboardPropertyInfo GetPropertyInfo(string eventName, string fieldName)
    {
        return metadata[eventName].Values.FirstOrDefault(x => x.FieldName == fieldName);
    }

    public StoryboardViewModelBase CreateObject(StoryboardMessage data)
    {
        switch (data.Name)
        {
            case TestSbioObject.Name:
                return new TestSbioObject(data);
            default:
                return null;
        }
    }

    public static StoryboardMetadata EventMetadata
    {
        get
        {
            if (eventMetadata == null)
                eventMetadata = new();
            return eventMetadata;
        }
    }

    StoryboardMetadata()
    {
        metadata.Add(TestSbioObject.Name, new Dictionary<string, StoryboardPropertyInfo>()
        {
            {nameof(TestSbioObject.ValueFloat), new(1, SBFieldType.Float_4f, 0, 4, 1, nameof(TestSbioObject.ValueFloat)) },
            {nameof(TestSbioObject.ValueInt64), new(2, SBFieldType.SignedInt64_8s, 4, 8, 1, nameof(TestSbioObject.ValueInt64)) },
            {nameof(TestSbioObject.ValueInt32), new(3, SBFieldType.SignedInt32_4s, 12, 4, 1, nameof(TestSbioObject.ValueInt32)) },
            {nameof(TestSbioObject.ValueInt16), new(4, SBFieldType.SignedInt16_2s, 16, 2, 1, nameof(TestSbioObject.ValueInt16)) },
            {nameof(TestSbioObject.ValueInt8), new(5, SBFieldType.SignedInt8_1s, 18, 1, 1, nameof(TestSbioObject.ValueInt8)) },
            {nameof(TestSbioObject.ValueUInt64), new(6, SBFieldType.Int64_8u, 19, 8, 1, nameof(TestSbioObject.ValueUInt64)) },
            {nameof(TestSbioObject.ValueUInt32), new(7, SBFieldType.Int32_4u, 27, 4, 1, nameof(TestSbioObject.ValueUInt32)) },
            {nameof(TestSbioObject.ValueUInt16), new(8, SBFieldType.Int16_2u, 31, 2, 1, nameof(TestSbioObject.ValueUInt16)) },
            {nameof(TestSbioObject.ValueUInt8), new(9, SBFieldType.Int8_1u, 33, 1, 1, nameof(TestSbioObject.ValueUInt8)) },
            {nameof(TestSbioObject.ValueIntArray), new(10, SBFieldType.SignedInt32_4s, 34, 4, 5, nameof(TestSbioObject.ValueIntArray)) },
            {nameof(TestSbioObject.ValueString), new(11, SBFieldType.String_1s, 54, 1, 1, nameof(TestSbioObject.ValueString)) },
        });
    }
}
