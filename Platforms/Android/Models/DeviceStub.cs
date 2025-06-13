using System;
using Android.OS;
using Android.Runtime;
using Java.Interop;
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Android.Models
{
    public class DeviceStub : Java.Lang.Object, IDevice, IParcelable
    {
        public DeviceStub(string name, string address)
        {
            Name = name;
            Address = address;
            Id = Guid.NewGuid();
        }

        public DeviceStub(Parcel parcel)
        {
            Name = parcel.ReadString();
            Address = parcel.ReadString();
            Id = Guid.Parse(parcel.ReadString() ?? Guid.NewGuid().ToString());
        }

        public string Name { get; }
        public string Address { get; }
        public Guid Id { get; }
        public DeviceState State => DeviceState.Disconnected;

        public int DescribeContents() => 0;

        public void WriteToParcel(Parcel dest, [GeneratedEnum] ParcelableWriteFlags flags)
        {
            dest.WriteString(Name);
            dest.WriteString(Address);
            dest.WriteString(Id.ToString());
        }

        public static readonly IParcelableCreator CREATOR = new DeviceStubCreator();

        private class DeviceStubCreator : Java.Lang.Object, IParcelableCreator
        {
            public Java.Lang.Object CreateFromParcel(Parcel source)
            {
                return new DeviceStub(source); // Теперь совместимо с Java.Lang.Object
            }

            public Java.Lang.Object[] NewArray(int size)
            {
                return new DeviceStub[size];
            }
        }
    }
}