
using Ahsoka.ServiceFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Utility;

/// <summary>
/// Utilities for Serializing and Deserializing JSON Text
/// </summary>
public static class JsonUtility
{
    /// <summary>
    /// Intialize All JsonConvert callse to resolve CamelCase for compatibility with 
    /// Javascript / Proto naming.
    /// </summary>
    static JsonUtility()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    /// <summary>
    /// Deserialize to Object
    /// </summary>
    /// <typeparam name="T">Type of Object to Deserialize</typeparam>
    /// <param name="json">Json Text</param>
    /// <returns></returns>
    public static T Deserialize<T>(string json) where T : class, new()
    {
        if (typeof(ICanDeserializeBase).IsAssignableFrom(typeof(T)))
        {
            T value = new();
            DeserializeWithReader<T>(json, value);
            return value;
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// Serialize to String
    /// </summary>
    /// <param name="value">Object to Serialize</param>
    /// <returns></returns>
    public static string Serialize(object value)
    {
        return JsonConvert.SerializeObject(value);
    }

    private static void DeserializeWithReader<T>(string jsonText, T rootObj)
    {
        ICanDeserializeBase currentObject = rootObj as ICanDeserializeBase;

        // Init Stack
        Stack<ICanDeserializeBase> stack = new();
        Stack<string> propertyStack = new();

        StringReader tx = new(jsonText);
        JsonTextReader reader = new(tx);
        string propertyName = string.Empty;
        int depth = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                propertyName = reader.Value?.ToString();
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                stack.Push(currentObject);
                propertyStack.Push(propertyName);

                if (!string.IsNullOrEmpty(propertyName)) // Root Object...ignore
                {
                    propertyStack.Push(propertyName); // Push Collection Property
                    currentObject = currentObject.CreatePropertyObject(propertyName);
                }

                AhsokaLogging.LogMessage(AhsokaVerbosity.Developer, $"{new string(' ', depth)}Start Object: {propertyName}");
                depth++;
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                currentObject?.ParseComplete();

                depth--;
                currentObject = stack.Pop();
                propertyName = propertyStack.Pop();

                AhsokaLogging.LogMessage(AhsokaVerbosity.Developer, $"{new string(' ', depth)}End Object");
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Developer, $"{new string(' ', depth)}Start Array: {propertyName}");
                depth++;
            }
            else if (reader.TokenType == JsonToken.EndArray)
            {
                depth--;
                AhsokaLogging.LogMessage(AhsokaVerbosity.Developer, $"{new string(' ', depth)}End Array");
            }
            else if (reader.TokenType != JsonToken.Null)
            {
                currentObject?.SetProperty(propertyName, reader.Value.ToString());
                AhsokaLogging.LogMessage(AhsokaVerbosity.Developer, $"{new string(' ', depth)}Property: {propertyName}, Value: {reader.TokenType}:{reader.Value}");
            }
        }
    }
}

/// <summary>
/// Base Object for use when doing Non-Reflection DeSerialization
/// </summary>
public class ICanDeserializeBase
{
    internal virtual ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        return null;
    }

    internal virtual void ParseComplete()
    {

    }

    internal virtual void SetProperty(string propertyName, string value)
    {

    }
}
