using System;
using Android.OS;
using Android.Runtime;
using Java.Interop;
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Android.Models
{
    public class DeviceStub : IDevice, IParcelable
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

        public void SetJniIdentityHashCode(int value)
        {
            throw new NotImplementedException();
        }

        public void SetPeerReference(JniObjectReference reference)
        {
            throw new NotImplementedException();
        }

        public void SetJniManagedPeerState(JniManagedPeerStates value)
        {
            throw new NotImplementedException();
        }

        public void UnregisterFromRuntime()
        {
            throw new NotImplementedException();
        }

        public void DisposeUnlessReferenced()
        {
            throw new NotImplementedException();
        }

        public void Disposed()
        {
            throw new NotImplementedException();
        }

        public void Finalized()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        [ExportField("CREATOR")]
        public static IParcelableCreator Creator => new DeviceStubCreator();

        public nint Handle => throw new NotImplementedException();

        public int JniIdentityHashCode => throw new NotImplementedException();

        public JniObjectReference PeerReference => throw new NotImplementedException();

        public JniPeerMembers JniPeerMembers => throw new NotImplementedException();

        public JniManagedPeerStates JniManagedPeerState => throw new NotImplementedException();

        private class DeviceStubCreator : Java.Lang.Object, IParcelableCreator
        {
            public Java.Lang.Object CreateFromParcel(Parcel source)
            {
                return new DeviceStub(source);
            }

            public Java.Lang.Object[] NewArray(int size)
            {
                return new DeviceStub[size];
            }
        }
    }
}