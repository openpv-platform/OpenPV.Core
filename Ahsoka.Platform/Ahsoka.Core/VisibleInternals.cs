using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenPV.DeveloperToolkit")]
[assembly: InternalsVisibleTo("Ahsoka.Test")]
[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth.LE")]
[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth")]
[assembly: InternalsVisibleTo("Ahsoka.Core.Cygnus")]
[assembly: InternalsVisibleTo("Ahsoka.Core.CAN")]

namespace Ahsoka;
internal class VisibleInternals
{

}
