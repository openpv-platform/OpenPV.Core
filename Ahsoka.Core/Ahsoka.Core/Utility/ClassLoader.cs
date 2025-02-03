using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ahsoka.Core.Utility;

/// <summary>
/// Services to support loading classes dynamicly via reflection.
/// </summary>
public static class ClassLoader
{
    private static readonly List<WeakReference<Assembly>> assemblies = new();
    private static readonly object syncRoot = new();

    #region Methods
    static ClassLoader()
    {
        lock (syncRoot)
        {
            var tempList = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("Ahsoka."));
            foreach (var item in tempList)
                assemblies.Add(new WeakReference<Assembly>(item));

            var asm = Assembly.GetEntryAssembly();
            if (!tempList.Contains(asm))
                assemblies.Add(new WeakReference<Assembly>(asm));
        }
    }

    /// <summary>
    /// Add an Assembly to the Search List
    /// </summary>
    /// <param name="assembly"></param>
    public static void AddAssembly(Assembly assembly)
    {
        lock (syncRoot)
            assemblies.Add(new WeakReference<Assembly>(assembly));
    }

    /// <summary>
    /// Add an Assembly to the Search List
    /// </summary>
    /// <param name="assembly"></param>
    public static void RemoveAssembly(Assembly assembly)
    {
        lock (syncRoot)
            foreach (var item in assemblies.ToArray())
                if (item.TryGetTarget(out var asm) && asm == assembly)
                    assemblies.Remove(item);
    }

    internal static IReadOnlyList<WeakReference<Assembly>> GetSystemAssemblies()
    {
        lock (syncRoot)
            return assemblies.ToArray();
    }

    internal static Type[] GetSubclasses(Type item)
    {
        List<Type> typeList = new();
        foreach (var asmwf in GetSystemAssemblies())
        {
            try
            {
                if (asmwf.TryGetTarget(out Assembly asm))
                    foreach (Type curType in asm.GetTypes())
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
            foreach (var asmwf in GetSystemAssemblies())
            {
                try
                {
                    if (asmwf.TryGetTarget(out Assembly asm))
                        foreach (Type curType in asm.GetTypes())
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
        foreach (var asmwf in GetSystemAssemblies())
        {
            try
            {
                if (asmwf.TryGetTarget(out Assembly asm))
                    foreach (Type curType in asm.GetTypes())
                    {
                        var attr = curType.GetCustomAttribute<T>();
                        if (attr != null && !curType.IsAbstract && !curType.IsInterface)
                            typeList[attr] = curType;
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
