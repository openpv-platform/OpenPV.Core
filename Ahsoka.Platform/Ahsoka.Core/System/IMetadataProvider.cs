using System.Collections.Generic;

namespace Ahsoka.System;

/// <summary>
/// Interface for Generated Object Metadata
/// </summary>
/// <typeparam name="KeyT">Type of Outer Dictionary Key</typeparam>
/// <typeparam name="KeyIT">Type of Internal Dictionary Key</typeparam>
/// <typeparam name="ObjectBase">Type of Base Class for Generated Objects</typeparam>
/// <typeparam name="PropertyInfoT">Type for Generated Properties</typeparam>
/// <typeparam name="Message">Type for Message Data</typeparam>
public interface IMetadataProvider<KeyT, KeyIT, ObjectBase, PropertyInfoT, Message>
{
    /// <summary>
    ///  Returns the entire Metadata Dictionary
    /// </summary>
    Dictionary<KeyT, Dictionary<KeyIT, PropertyInfoT>> GetMetadata();

    /// <summary>
    ///  Returns the Properties of a Single Generated Object
    /// </summary>
    /// <param name="keyValue">Key of the Generated Object</param>
    Dictionary<KeyIT, PropertyInfoT> GetMetadata(KeyT keyValue);

    /// <summary>
    ///  Constructs a Generated Object Based on a Data Object
    /// </summary>
    /// <param name="data">Object containing the information for construction</param>
    ObjectBase CreateObject(Message data);
}