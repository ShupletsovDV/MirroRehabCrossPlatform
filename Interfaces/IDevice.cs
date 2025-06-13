using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirroRehab.Interfaces
{
    public interface IDevice
    {
        string Name { get; }
        string Address { get; }
        Guid Id { get; }
        DeviceState State { get; }
    }

    public enum DeviceState
    {
        Disconnected,
        Connected
    }
}
