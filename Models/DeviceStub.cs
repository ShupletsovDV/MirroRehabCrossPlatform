using System;
using MirroRehab.Interfaces;

namespace MirroRehab.Models
{
    public class DeviceStub : IDevice
    {
        public DeviceStub(string name, string address)
        {
            Name = name;
            Address = address;
            Id = Guid.NewGuid();
        }

        public string Name { get; }
        public string Address { get; }
        public Guid Id { get; }
        public DeviceState State => DeviceState.Disconnected;
    }
}