/*using Android.Bluetooth;
using System;
#if ANDROID
using Android.OS;
using Android.Runtime;
#endif
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Android
{
    public class BluetoothDeviceWrapper : IDevice
#if ANDROID
        , IParcelable
#endif
    {
        private readonly BluetoothDevice _device;

        public BluetoothDeviceWrapper(BluetoothDevice device)
        {
            _device = device;
        }

#if ANDROID
        public BluetoothDeviceWrapper(Parcel parcel)
        {
            Name = parcel.ReadString();
            Address = parcel.ReadString();
            Id = Guid.Parse(parcel.ReadString() ?? Guid.NewGuid().ToString());
            // BluetoothDevice не восстанавливается, используем заглушку
        }
#endif

        public string Name => _device.Name;
        public string Address => _device.Address;
        public Guid Id => Guid.Parse(_device.Address.Replace(":", "").PadRight(32, '0').Substring(0, 32));
        public DeviceState State => _device.BondState == Bond.Bonded ? DeviceState.Connected : DeviceState.Disconnected;

#if ANDROID
        public int DescribeContents() => 0;

        public void WriteToParcel(Parcel dest, [GeneratedEnum] ParcelableWriteFlags flags)
        {
            dest.WriteString(Name);
            dest.WriteString(Address);
            dest.WriteString(Id.ToString());
        }

        [ExportField("CREATOR")]
        public static IParcelableCreator Creator => new BluetoothDeviceWrapperCreator();

        private class BluetoothDeviceWrapperCreator : Java.Lang.Object, IParcelableCreator
        {
            public Java.Lang.Object CreateFromParcel(Parcel source)
            {
                return new DeviceStub(source.ReadString(), source.ReadString());
            }

            public Java.Lang.Object[] NewArray(int size)
            {
                return new DeviceStub[size];
            }
        }
#endif
    }
}*/