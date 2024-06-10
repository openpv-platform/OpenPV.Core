using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Core;

internal abstract class PropertiesBase<TV>
{
    protected readonly TV Properties;

    protected PropertiesBase(ObjectPath objectPath, TV properties)
    {
        ObjectPath = objectPath;
        Properties = properties;
    }

    public ObjectPath ObjectPath { get; }

    public Task<object> GetAsync(string prop)
    {
        return Task.FromResult(Properties.ReadProperty(prop));
    }

    public Task<T> GetAsync<T>(string prop)
    {
        return Task.FromResult(Properties.ReadProperty<T>(prop));
    }

    public Task<TV> GetAllAsync()
    {
        return Task.FromResult(Properties);
    }

    public Task SetAsync(string prop, object val)
    {
        return Properties.SetProperty(prop, val);
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        return SignalWatcher.AddAsync(this, nameof(OnPropertiesChanged), handler);
    }

    public event Action<PropertyChanges> OnPropertiesChanged;

    public Task TriggerPropertyChanged(string prop)
    {
        // TODO: Not sure I'm entirely in love with how I'm having to implement this.
        OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty(prop, Properties.ReadProperty<byte[]>(prop)));
        return Task.CompletedTask;
    }
}

internal static class PropertyAccessExtensions
{
    public static T ReadProperty<T>(this object o, string prop)
    {
        var propertyValue = o.GetType().GetProperty(prop)?.GetValue(o);
        return (T)propertyValue;
    }

    public static object ReadProperty(this object o, string prop)
    {
        return o.GetType().GetProperty(prop)?.GetValue(o);
    }


    public static Task SetProperty(this object o, string prop, object val)
    {
        o.GetType().GetProperty(prop)?.SetValue(o, val);
        return Task.CompletedTask;
    }
}