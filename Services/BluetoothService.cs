using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using MirroRehab.Interfaces;

namespace MirroRehab.Services
{
    public class BluetoothService : IBluetoothService
    {
        private readonly IBluetoothService _platformService;

        public BluetoothService(IBluetoothService platformService)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        }

        public bool IsConnected => _platformService.IsConnected;

        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            try
            {
                return await _platformService.DiscoverMirroRehabDevicesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска устройств: {ex.Message}");
                throw;
            }
        }

        public async Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId)
        {
            try
            {
                return await _platformService.ConnectToDeviceAsync(deviceNameOrId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                throw;
            }
        }

        public async Task SendDataAsync(string data)
        {
            try
            {
                await _platformService.SendDataAsync(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отправки данных: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectDeviceAsync()
        {
            try
            {
                await _platformService.DisconnectDeviceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отключения: {ex.Message}");
                throw;
            }
        }

        public void DisconnectDevice()
        {
            Task.Run(DisconnectDeviceAsync).GetAwaiter().GetResult();
        }

        
    }
}