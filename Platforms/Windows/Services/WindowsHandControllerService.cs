using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MirroRehab.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MirroRehab.Platforms.Windows.Services
{
    public class WindowsHandControllerService
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly IUdpClientService _udpClientService;
        private readonly IPositionProcessor _positionProcessor;

        public WindowsHandControllerService()
        {
            var serviceProvider = MauiProgram.CreateMauiApp().Services;
            _bluetoothService = serviceProvider.GetRequiredService<IBluetoothService>();
            _udpClientService = serviceProvider.GetRequiredService<IUdpClientService>();
            _positionProcessor = serviceProvider.GetRequiredService<IPositionProcessor>();
        }

        public async Task<bool> StartTracking(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не выбрано");
                    return false;
                }

                if (!_bluetoothService.IsConnected)
                {
                    Debug.WriteLine($"Подключение к {device.Name}...");
                    await _bluetoothService.ConnectToDeviceAsync(device.Address);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    await _udpClientService.StartPingAsync();
                    var receiveData = await _udpClientService.ReceiveDataAsync();
                    Debug.WriteLine($"Данные получены: {receiveData?.type}");

                    if (receiveData != null && receiveData.type == "position")
                    {
                        await _positionProcessor.ProcessPositionAsync(receiveData, _bluetoothService);
                    }
                    receiveData = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отслеживания на Windows: {ex.Message}");
                return false;
            }
        }
    }
}