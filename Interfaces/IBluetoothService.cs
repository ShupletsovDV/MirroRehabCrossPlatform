
namespace MirroRehab.Interfaces
{
    public interface IBluetoothService
    {
        bool IsConnected { get; }
        Task<List<IDevice>> DiscoverMirroRehabDevicesAsync(); 
        Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId);
        Task SendDataAsync(string data);
        Task DisconnectDeviceAsync();
        void DisconnectDevice();


    }
}