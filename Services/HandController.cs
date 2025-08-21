using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MirroRehab.Interfaces;
using Microsoft.Maui.Controls;

namespace MirroRehab.Services
{
    public class HandController
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly IHandController _platformHandController;
        private readonly Dictionaries _dictionaries;

        public event EventHandler<string> TrackingDataReceived;
        public event EventHandler<(string Message, Color Color)> DemoStatusUpdated;

        public HandController(
            IBluetoothService bluetoothService,
            IHandController platformHandController)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            _platformHandController = platformHandController ?? throw new ArgumentNullException(nameof(platformHandController));
            _dictionaries = new Dictionaries();
            _dictionaries.UpdateDictionaries();
        }

        public async Task<bool> CalibrateDevice(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не выбрано");
                    return false;
                }

                return await _platformHandController.CalibrateDevice(cancellationToken, device);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка калибровки: {ex.Message}");
                return false;
            }
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

                return await _platformHandController.StartTrackingAsync(cancellationToken, device);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отслеживания: {ex.Message}");
                return false;
            }
        }

        public  async Task DemoMirro(CancellationToken cancellationToken, IDevice device)
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
            }
            finally
            {
                await _bluetoothService.DisconnectDeviceAsync();
                UpdateViewModel("Соединение закрыто", Colors.Black);
            }
        }


        public static async Task DemoMirro(CancellationToken cancellationToken, IDevice device, IBluetoothService bluetoothService, Dictionaries dictionaries)
        {
            int sleep = 50;

            try
            {
                // Логика проверки подключения
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не выбрано");
                    return;
                }

                if (!bluetoothService.IsConnected)
                {
                    Debug.WriteLine($"Подключение к устройству {device.Name}");
                    await bluetoothService.ConnectToDeviceAsync(device.Address);
                }

                if (dictionaries.DictIndex == null || dictionaries.DictMiddle == null || dictionaries.DictRing == null || dictionaries.DictPinky == null)
                {
                    throw new InvalidOperationException("Словари не инициализированы");
                }

                Debug.WriteLine("Демо запущено");

                while (!cancellationToken.IsCancellationRequested)
                {
                    sleep = new Random().Next(10, 60);

                    // Сжатие всех пальцев
                    for (double i = 0.0; i <= 3.0; i += 0.1)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        double roundedI = Math.Round(i, 1);
                        if (!dictionaries.DictIndex.ContainsKey(roundedI) || !dictionaries.DictMiddle.ContainsKey(roundedI) || !dictionaries.DictRing.ContainsKey(roundedI) || !dictionaries.DictPinky.ContainsKey(roundedI))
                        {
                            Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                            continue;
                        }

                        string defaultData = $"{dictionaries.DictIndex[roundedI]},{dictionaries.DictMiddle[roundedI]},{dictionaries.DictRing[roundedI]},{dictionaries.DictPinky[roundedI]},0";
                        await bluetoothService.SendDataAsync(defaultData);
                        await Task.Delay(sleep, cancellationToken);
                    }

                    // Разжатие всех пальцев
                    for (double i = 3.0; i >= 0.0; i -= 0.1)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        double roundedI = Math.Round(i, 1);
                        if (!dictionaries.DictIndex.ContainsKey(roundedI) || !dictionaries.DictMiddle.ContainsKey(roundedI) || !dictionaries.DictRing.ContainsKey(roundedI) || !dictionaries.DictPinky.ContainsKey(roundedI))
                        {
                            Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                            continue;
                        }

                        string defaultData = $"{dictionaries.DictIndex[roundedI]},{dictionaries.DictMiddle[roundedI]},{dictionaries.DictRing[roundedI]},{dictionaries.DictPinky[roundedI]},0";
                        await bluetoothService.SendDataAsync(defaultData);
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
                            if (!dictionaries.DictIndex.ContainsKey(roundedI) || !dictionaries.DictMiddle.ContainsKey(roundedI) || !dictionaries.DictRing.ContainsKey(roundedI) || !dictionaries.DictPinky.ContainsKey(roundedI))
                            {
                                Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                                continue;
                            }

                            string a1 = finger == 0 ? $"{dictionaries.DictIndex[roundedI]}" : $"{dictionaries.DictIndex[0.0]}";
                            string b1 = finger == 1 ? $"{dictionaries.DictMiddle[roundedI]}" : $"{dictionaries.DictMiddle[0.0]}";
                            string c1 = finger == 2 ? $"{dictionaries.DictRing[roundedI]}" : $"{dictionaries.DictRing[0.0]}";
                            string f1 = finger == 3 ? $"{dictionaries.DictPinky[roundedI]}" : $"{dictionaries.DictPinky[0.0]}";
                            string defaultData = $"{a1},{b1},{c1},{f1},0";
                            await bluetoothService.SendDataAsync(defaultData);
                            await Task.Delay(sleep, cancellationToken);
                        }

                        for (double i = 3.0; i >= 0.0; i -= 0.1)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            double roundedI = Math.Round(i, 1);
                            if (!dictionaries.DictIndex.ContainsKey(roundedI) || !dictionaries.DictMiddle.ContainsKey(roundedI) || !dictionaries.DictRing.ContainsKey(roundedI) || !dictionaries.DictPinky.ContainsKey(roundedI))
                            {
                                Debug.WriteLine($"Ключ {roundedI} отсутствует в словарях");
                                continue;
                            }

                            string a1 = finger == 0 ? $"{dictionaries.DictIndex[roundedI]}" : $"{dictionaries.DictIndex[0.0]}";
                            string b1 = finger == 1 ? $"{dictionaries.DictMiddle[roundedI]}" : $"{dictionaries.DictMiddle[0.0]}";
                            string c1 = finger == 2 ? $"{dictionaries.DictRing[roundedI]}" : $"{dictionaries.DictRing[0.0]}";
                            string f1 = finger == 3 ? $"{dictionaries.DictPinky[roundedI]}" : $"{dictionaries.DictPinky[0.0]}";
                            string defaultData = $"{a1},{b1},{c1},{f1},0";
                            await bluetoothService.SendDataAsync(defaultData);
                            await Task.Delay(sleep, cancellationToken);
                        }
                    }
                }

                Debug.WriteLine("Демо остановлено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в демо: {ex.Message}");
            }
            finally
            {
                await bluetoothService.DisconnectDeviceAsync();
                Debug.WriteLine("Соединение закрыто");
            }
        }

        private void UpdateViewModel(string message, Color color)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TrackingDataReceived?.Invoke(this, $"handController: {message}");
                DemoStatusUpdated?.Invoke(this, (message, color));
            });
        }
    }
}