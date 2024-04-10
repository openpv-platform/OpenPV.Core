using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ahsoka.Utility;

/// <summary>
/// Services to support loading classes dynamicly via reflection.
/// </summary>
public static class ClassLoader
{
    private static readonly List<Assembly> assemblies;
    private static readonly object syncRoot = new();

    #region Methods
    static ClassLoader()
    {
        lock (syncRoot)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("Ahsoka.")).ToList();
            var asm = Assembly.GetEntryAssembly();
            if (!assemblies.Contains(asm))
                assemblies.Add(asm);
        }
    }

    /// <summary>
    /// Add an Assembly to the Search List
    /// </summary>
    /// <param name="assembly"></param>
    public static void AddAssembly(Assembly assembly)
    {
        lock (syncRoot)
            if (!assemblies.Contains(assembly))
                assemblies.Add(assembly);
    }

    internal static IReadOnlyList<Assembly> GetSystemAssemblies()
    {
        lock (syncRoot)
            return assemblies.ToArray();
    }

    internal static Type[] GetSubclasses(Type item)
    {
        List<Type> typeList = new();
        foreach (Assembly assm in GetSystemAssemblies())
        {
            try
            {
                foreach (Type curType in assm.GetTypes())
                {
                    if (curType.IsSubclassOf(item))
                        typeList.Add(curType);
                }
            }
            catch (ReflectionTypeLoadException)
            {

            }
        }
        return typeList.ToArray();
    }

    internal static Type[] GetItemsWithInterface(Type item)
    {
        List<Type> typeList = new();
        if (item != null)
        {
            foreach (Assembly assm in GetSystemAssemblies())
            {
                try
                {
                    foreach (Type curType in assm.GetTypes())
                    {
                        if (item.IsAssignableFrom(curType) && !curType.IsAbstract && !curType.IsInterface)
                            typeList.Add(curType);
                    }
                }
                catch (ReflectionTypeLoadException)
                {

                }
            }
        }
        return typeList.ToArray();
    }

    /// <summary>
    /// Enumerate Types with the specified attribute using Reflection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Dictionary<T, Type> GetItemsWithAttribute<T>() where T : Attribute
    {
        Dictionary<T, Type> typeList = new();
        foreach (Assembly assm in GetSystemAssemblies())
        {
            try
            {
                foreach (Type curType in assm.GetTypes())
                {
                    var attr = curType.GetCustomAttribute<T>();
                    if (attr != null && !curType.IsAbstract && !curType.IsInterface)
                        typeList.Add(attr, curType);
                }
            }
            catch (ReflectionTypeLoadException)
            {

            }
        }

        return typeList;
    }

    internal static Object CreateType(Type curRuleType, params object[] args)
    {
        return Activator.CreateInstance(curRuleType, args);
    }

    /// <summary>
    /// Delays Construction until Field Value is Accessed Directly
    /// allowing construction to occurr only if needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parent"></param>
    /// <param name="fieldValue"></param>
    /// <returns></returns>
    public static T LazyLoad<T>(this object parent, ref T fieldValue) where T : class, new()
    {
        lock (parent)
            fieldValue ??= new T();

        return fieldValue;

    }
    #endregion
}
