using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonText = System.Text.Json;

namespace Ahsoka.Utility;

/// <summary>
/// Utilities for Serializing and Deserializing JSON Text
/// </summary>
public static class JsonUtility
{
    internal static bool IsTesting { get; set; }

    internal static JsonSerializerOptions Options { get => options; set => options = value; }

    static JsonSerializerOptions options;

    /// <summary>
    /// Intialize All JsonConvert callse to resolve CamelCase for compatibility with 
    /// Javascript / Proto naming.
    /// </summary>
    static JsonUtility()
    {
        Options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
        };

        Options.Converters.Add(new JsonStringEnumConverter(JsonText.JsonNamingPolicy.CamelCase, true));
        Options.Converters.Add(new BooleanConverter());

    }

    /// <summary>
    /// Deserialize to Object
    /// </summary>
    /// <typeparam name="T">Type of Object to Deserialize</typeparam>
    /// <param name="json">Json Text</param>
    /// <returns></returns>
    public static T Deserialize<T>(string json) where T : class, new()
    {
        /*if (OperatingSystem.IsLinux() || IsTesting)
        {
            if (typeof(ICanDeserializeBase).IsAssignableFrom(typeof(T)))
            {
                T value = new();
                DeserializeWithReader<T>(json, value);
                return value;
            }
        }*/

        return JsonText.JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Serialize to String
    /// </summary>
    /// <param name="value">Object to Serialize</param>
    /// <returns></returns>
    public static string Serialize(object value)
    {
        return JsonText.JsonSerializer.Serialize(value, Options);
    }

    /*private static void DeserializeWithReader<T>(string jsonText, T rootObj)
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
                    currentObject = currentObject?.CreatePropertyObject(propertyName);
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
    }*/
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

internal class BooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.String:
                return reader.GetString() switch
                {
                    "true" => true,
                    "false" => false,
                    _ => throw new JsonException()
                };
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}