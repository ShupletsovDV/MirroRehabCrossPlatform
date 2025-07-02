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

        public WindowsHandControllerService(
            IBluetoothService bluetoothService,
            IUdpClientService udpClientService,
            IPositionProcessor positionProcessor)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            _udpClientService = udpClientService ?? throw new ArgumentNullException(nameof(udpClientService));
            _positionProcessor = positionProcessor ?? throw new ArgumentNullException(nameof(positionProcessor));
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

                // Не вызываем повторное подключение здесь!
                Debug.WriteLine("Соединение предполагается уже открытым, повторное подключение не требуется.");


                while (!cancellationToken.IsCancellationRequested)
                {
                    await _udpClientService.PingSensoAsync();
                    var receiveData = await _udpClientService.ReceiveDataAsync(cancellationToken);
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
                await _bluetoothService.DisconnectDeviceAsync();
                return false;
            }
        }

    }
}