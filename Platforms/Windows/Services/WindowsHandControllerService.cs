using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Windows.Services
{
    public class WindowsHandControllerService : IHandController
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

        public async Task<bool> StartTrackingAsync(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не выбрано");
                    return false;
                }

                Debug.WriteLine("Соединение предполагается уже открытым, повторное подключение не требуется.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    
                    var receiveData = await _udpClientService.ReceiveDataAsync(cancellationToken);
                    Debug.WriteLine($"[WINDOWSCONTROLLER] Данные получены: {receiveData?.type}");

                    if (receiveData != null && receiveData.type == "position")
                    {
                        await _positionProcessor.ProcessPositionAsync(receiveData, _bluetoothService);
                    }
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


        // Пустые реализации для CalibrateDevice и DemoMirro, так как они обрабатываются в HandController
        public Task<bool> CalibrateDevice(CancellationToken cancellationToken,IDevice device)
        {
            throw new NotSupportedException("Калибровка должна выполняться через основной HandController.");
        }

        public Task DemoMirro(CancellationToken cancellationToken, IDevice device)
        {
            throw new NotSupportedException("Демо должно выполняться через основной HandController.");
        }
    }
}