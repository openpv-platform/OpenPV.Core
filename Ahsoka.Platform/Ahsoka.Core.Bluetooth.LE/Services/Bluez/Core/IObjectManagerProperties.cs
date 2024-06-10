using System.Collections.Generic;

namespace Ahsoka.Services.Bluez.Core;

internal interface IObjectManagerProperties
{
    IDictionary<string, IDictionary<string, object>> GetProperties();
}