using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JsonText = System.Text.Json;

namespace Ahsoka.Core.Utility;

/// <summary>
/// Utilities for Serializing and Deserializing JSON Text
/// </summary>
public static class JsonUtility
{
    internal static bool IsTesting { get; set; }

    internal static JsonSerializerOptions Options { get => options; }

    /// <summary>
    /// List of Type Resolvers
    /// </summary>
    static List<IJsonTypeInfoResolver> registeredTypeResolvers = new();

    static JsonSerializerOptions options;
    static JsonSerializerOptions typeOptions;

    /// <summary>
    /// Intialize All JsonConvert callse to resolve CamelCase for compatibility with 
    /// Javascript / Proto naming.
    /// </summary>
    static JsonUtility()
    {
        // Used for Reflection Serialization
        options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        };

        options.Converters.Add(new JsonStringEnumConverter(null, true));
        options.Converters.Add(new BooleanConverter());

        // Add Default Type Resolver
        AddTypeResolver(CoreJsonSerializerContext.Default);

    }

    /// <summary>
    /// Add Type Resolver to Internal List
    /// </summary>
    /// <param name="typeResolver"></param>
    public static void AddTypeResolver(IJsonTypeInfoResolver typeResolver)
    {
        if (typeResolver != null)
            if (!registeredTypeResolvers.Contains(typeResolver))
            {
                lock (registeredTypeResolvers)
                {
                    registeredTypeResolvers.Add(typeResolver);

                    // Used for Code Generation Serialization
                    typeOptions = new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                        IncludeFields = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
                    };

                    typeOptions.Converters.Add(new JsonStringEnumConverter(null, true));
                    typeOptions.Converters.Add(new BooleanConverter());

                    foreach(var item in registeredTypeResolvers)
                        typeOptions.TypeInfoResolverChain.Add(item);
                }
            }
    }

    /// <summary>
    /// Deserialize to Object
    /// </summary>
    /// <typeparam name="T">Type of Object to Deserialize</typeparam>
    /// <param name="json">Json Text</param>
    /// <returns></returns>
    public static T Deserialize<T>(string json) where T : class, new()
    {
        return JsonText.JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Deserialize to Object with Source Generated TypeInfo
    /// </summary>
    /// <typeparam name="T">Type of Object to Deserialize</typeparam>
    /// <param name="json">Json Text</param>
    /// <returns></returns>
    public static T DeserializeWithTypeInfo<T>(string json) where T : class, new()
    {
        return JsonSerializer.Deserialize<T>(json, typeOptions);
    }

    /// <summary>
    /// Serialize to String
    /// </summary>
    /// <param name="value">Object to Serialize</param>
    /// <returns></returns>
    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, Options);
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