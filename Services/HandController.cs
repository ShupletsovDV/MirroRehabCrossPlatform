using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MirroRehab.Interfaces;
using MirroRehab.ViewModels;

namespace MirroRehab.Services
{
    public class HandController : IHandController
    {
        private static HandController _instance;
        private readonly IBluetoothService _bluetoothService;
        private readonly IUdpClientService _udpClientService;
        private readonly IPositionProcessor _positionProcessor;
        private readonly MainPageViewModel _viewModel;
        private readonly Dictionaries _dictionaries;

        public static HandController GetHandController(
            IBluetoothService bluetoothService,
            IUdpClientService udpClientService,
            IPositionProcessor positionProcessor,
            MainPageViewModel viewModel = null)
        {
            return _instance ??= new HandController(bluetoothService, udpClientService, positionProcessor, viewModel);
        }

        public HandController(
            IBluetoothService bluetoothService,
            IUdpClientService udpClientService,
            IPositionProcessor positionProcessor,
            MainPageViewModel viewModel = null)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            _udpClientService = udpClientService ?? throw new ArgumentNullException(nameof(udpClientService));
            _positionProcessor = positionProcessor ?? throw new ArgumentNullException(nameof(positionProcessor));
            _viewModel = viewModel;
            _dictionaries = new Dictionaries();
            _dictionaries.UpdateDictionaries();
        }

        public async Task<bool> CalibrateDevice(IDevice device)
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

                Debug.WriteLine($"Калибровка устройства {device.Name} успешна");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка калибровки: {ex.Message}");
                return false;
            }
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

#if ANDROID
                // Платформоспецифичная логика для Android
                var platformService = new AndroidHandControllerService();
                return await platformService.StartTracking(cancellationToken, device);
#else
                // Платформоспецифичная логика для Windows (или других платформ)
                return await StartTrackingInternal(cancellationToken, device);
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отслеживания: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> StartTrackingInternal(CancellationToken cancellationToken, IDevice device)
        {
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

        public async Task DemoMirro(CancellationToken cancellationToken, IDevice device)
        {
            int sleep = 50;

            try
            {
                UpdateViewModel("Проверка подключения к устройству...", Colors.Black);

                if (device == null)
                {
                    throw new InvalidOperationException("Устройство не выбрано");
                }

                if (!_bluetoothService.IsConnected)
                {
                    UpdateViewModel($"Подключение к {device.Name}...", Colors.Black);
                    Debug.WriteLine($"Подключение к устройству {device.Name}");
                    await _bluetoothService.ConnectToDeviceAsync(device.Address);
                }

                if (_dictionaries.DictIndex == null || _dictionaries.DictMiddle == null ||
                    _dictionaries.DictRing == null || _dictionaries.DictPinky == null)
                {
                    throw new InvalidOperationException("Словари не инициализированы");
                }

                UpdateViewModel("Демо запущено", Colors.Green);

                while (!cancellationToken.IsCancellationRequested)
                {
                    sleep = new Random().Next(10, 60);

                    // Сжатие всех пальцев
                    for (double i = 0.0; i <= 3.0; i += 0.1)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        double roundedI = Math.Round(i, 1);
                        if (!_dictionaries.DictIndex.ContainsKey(roundedI) ||
                            !_dictionaries.DictMiddle.ContainsKey(roundedI) ||
                            !_dictionaries.DictRing.ContainsKey(roundedI) ||
                            !_dictionaries.DictPinky.ContainsKey(roundedI))
                        {
                            Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                            continue;
                        }

                        string defaultData = $"{_dictionaries.DictIndex[roundedI]},{_dictionaries.DictMiddle[roundedI]},{_dictionaries.DictRing[roundedI]},{_dictionaries.DictPinky[roundedI]},0";
                        UpdateViewModel($"Команда: {defaultData}", Colors.Black);
                        await _bluetoothService.SendDataAsync(defaultData);
                        await Task.Delay(sleep, cancellationToken);
                    }

                    // Разжатие всех пальцев
                    for (double i = 3.0; i >= 0.0; i -= 0.1)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        double roundedI = Math.Round(i, 1);
                        if (!_dictionaries.DictIndex.ContainsKey(roundedI) ||
                            !_dictionaries.DictMiddle.ContainsKey(roundedI) ||
                            !_dictionaries.DictRing.ContainsKey(roundedI) ||
                            !_dictionaries.DictPinky.ContainsKey(roundedI))
                        {
                            Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                            continue;
                        }

                        string defaultData = $"{_dictionaries.DictIndex[roundedI]},{_dictionaries.DictMiddle[roundedI]},{_dictionaries.DictRing[roundedI]},{_dictionaries.DictPinky[roundedI]},0";
                        UpdateViewModel($"Команда: {defaultData}", Colors.Black);
                        await _bluetoothService.SendDataAsync(defaultData);
                        await Task.Delay(sleep, cancellationToken);
                    }

                    // Сжатие и разжатие пальцев по отдельности
                    for (int finger = 0; finger < 4; finger++)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        for (double i = 0.0; i <= 3.0; i += 0.1)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            double roundedI = Math.Round(i, 1);
                            if (!_dictionaries.DictIndex.ContainsKey(roundedI) ||
                                !_dictionaries.DictMiddle.ContainsKey(roundedI) ||
                                !_dictionaries.DictRing.ContainsKey(roundedI) ||
                                !_dictionaries.DictPinky.ContainsKey(roundedI))
                            {
                                Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                                continue;
                            }

                            string a1 = finger == 0 ? $"{_dictionaries.DictIndex[roundedI]}" : $"{_dictionaries.DictIndex[0.0]}";
                            string b1 = finger == 1 ? $"{_dictionaries.DictMiddle[roundedI]}" : $"{_dictionaries.DictMiddle[0.0]}";
                            string c1 = finger == 2 ? $"{_dictionaries.DictRing[roundedI]}" : $"{_dictionaries.DictRing[0.0]}";
                            string f1 = finger == 3 ? $"{_dictionaries.DictPinky[roundedI]}" : $"{_dictionaries.DictPinky[0.0]}";
                            string defaultData = $"{a1},{b1},{c1},{f1},0";
                            UpdateViewModel($"Команда: {defaultData}", Colors.Black);
                            await _bluetoothService.SendDataAsync(defaultData);
                            await Task.Delay(sleep, cancellationToken);
                        }

                        for (double i = 3.0; i >= 0.0; i -= 0.1)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            double roundedI = Math.Round(i, 1);
                            if (!_dictionaries.DictIndex.ContainsKey(roundedI) ||
                                !_dictionaries.DictMiddle.ContainsKey(roundedI) ||
                                !_dictionaries.DictRing.ContainsKey(roundedI) ||
                                !_dictionaries.DictPinky.ContainsKey(roundedI))
                            {
                                Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                                continue;
                            }

                            string a1 = finger == 0 ? $"{_dictionaries.DictIndex[roundedI]}" : $"{_dictionaries.DictIndex[0.0]}";
                            string b1 = finger == 1 ? $"{_dictionaries.DictMiddle[roundedI]}" : $"{_dictionaries.DictMiddle[0.0]}";
                            string c1 = finger == 2 ? $"{_dictionaries.DictRing[roundedI]}" : $"{_dictionaries.DictRing[0.0]}";
                            string f1 = finger == 3 ? $"{_dictionaries.DictPinky[roundedI]}" : $"{_dictionaries.DictPinky[0.0]}";
                            string defaultData = $"{a1},{b1},{c1},{f1},0";
                            UpdateViewModel($"Команда: {defaultData}", Colors.Black);
                            await _bluetoothService.SendDataAsync(defaultData);
                            await Task.Delay(sleep, cancellationToken);
                        }
                    }
                }

                UpdateViewModel("Демо остановлено", Colors.Green);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в демо: {ex.Message}");
                UpdateViewModel($"Ошибка: {ex.Message}", Colors.Yellow);
                throw;
            }
        }

        private void UpdateViewModel(string message, Color color)
        {
            if (_viewModel != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _viewModel.MessageInfo = message;
                    _viewModel.StatusColor = color;
                });
            }
        }
    }
}