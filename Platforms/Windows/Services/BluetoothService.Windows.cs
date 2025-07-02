using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using Microsoft.Maui.Devices;

namespace MirroRehab.Platforms.Windows.Services
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothDevice _connectedDevice;
        private RfcommDeviceService _service;
        private StreamSocket _socket;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public bool IsConnected => _connectedDevice != null && _socket != null;

        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            var result = new List<IDevice>();
            var knownDevices = new Dictionary<string, DeviceInformation>();

            try
            {
                var tcs = new TaskCompletionSource<bool>();

                const string aqsFilter =
                    "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";

                var watcher = DeviceInformation.CreateWatcher(
                    aqsFilter,
                    new[]
                    {
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Aep.IsPaired",
                "System.Devices.Aep.CanPair",
                "System.ItemNameDisplay"
                    },
                    DeviceInformationKind.AssociationEndpoint);

                watcher.Added += async (s, deviceInfo) =>
                {
                    lock (knownDevices)
                    {
                        knownDevices[deviceInfo.Id] = deviceInfo;
                    }

                    string name = deviceInfo.Name ?? string.Empty;
                    Debug.WriteLine($"[ADDED] '{name}' — {deviceInfo.Id}");

                    if (name.IndexOf("MirroRehab", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        await TryPairAsync(deviceInfo);

                        if (!result.Any(d => d.Address == deviceInfo.Id))
                        {
                            result.Add(new DeviceStub(name, deviceInfo.Id));
                        }
                    }
                };

                watcher.Updated += async (s, update) =>
                {
                    DeviceInformation updatedDevice = null;

                    lock (knownDevices)
                    {
                        if (knownDevices.TryGetValue(update.Id, out var existing))
                        {
                            existing.Update(update);
                            updatedDevice = existing;
                        }
                    }

                    if (updatedDevice != null)
                    {
                        string name = updatedDevice.Name ?? string.Empty;
                        Debug.WriteLine($"[UPDATED] '{name}' — {updatedDevice.Id}");

                        if (name.IndexOf("MirroRehab", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            await TryPairAsync(updatedDevice);

                            if (!result.Any(d => d.Address == updatedDevice.Id))
                            {
                                result.Add(new DeviceStub(name, updatedDevice.Id));
                            }
                        }
                    }
                };

                watcher.EnumerationCompleted += (s, _) =>
                {
                    Debug.WriteLine("Сканирование завершено.");
                    tcs.TrySetResult(true);
                };

                watcher.Stopped += (s, _) =>
                {
                    Debug.WriteLine("Сканирование остановлено.");
                    tcs.TrySetResult(true);
                };

                Debug.WriteLine("Поиск устройств MirroRehab начат…");
                watcher.Start();

                await Task.Delay(20000);
                watcher.Stop();
                await tcs.Task;

                Debug.WriteLine($"Найдено устройств MirroRehab: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в DeviceWatcher: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task TryPairAsync(DeviceInformation deviceInfo)
        {
            try
            {
                if (!deviceInfo.Pairing.IsPaired && deviceInfo.Pairing.CanPair)
                {
                    Debug.WriteLine($"→ Инициируется сопряжение с {deviceInfo.Name}");

                    var customPairing = deviceInfo.Pairing.Custom;

                    // Обработчик запроса
                    void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
                    {
                        Debug.WriteLine($"→ Запрошено подтверждение для {deviceInfo.Name}, тип: {args.PairingKind}");
                        args.Accept(); // Подтверждаем сопряжение
                    }

                    customPairing.PairingRequested += OnPairingRequested;

                    // Пытаемся спариться с автоматическим подтверждением
                    var result = await customPairing.PairAsync(DevicePairingKinds.ConfirmOnly);

                    customPairing.PairingRequested -= OnPairingRequested;

                    Debug.WriteLine($"→ Результат сопряжения с {deviceInfo.Name}: {result.Status}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка сопряжения с {deviceInfo.Name}: {ex.Message}");
            }
        }











        public async Task<IDevice> ConnectToDeviceAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_socket != null)
                {
                    Debug.WriteLine("Соединение уже открыто. Повторное подключение не требуется.");
                    return new DeviceStub(_connectedDevice.Name, deviceId);
                }

                if (IsConnected)
                {
                    Debug.WriteLine("Устройство уже подключено. Отключаем перед новым подключением.");
                    await DisconnectDeviceAsync();
                    await Task.Delay(500);
                }

                var deviceInfo = await DeviceInformation.CreateFromIdAsync(deviceId);
                if (deviceInfo == null)
                    throw new Exception("Устройство не найдено.");

                if (deviceInfo.Pairing != null && !deviceInfo.Pairing.IsPaired && deviceInfo.Pairing.CanPair)
                {
                    Debug.WriteLine($"→ Инициируется сопряжение с {deviceInfo.Name}");
                    var custom = deviceInfo.Pairing.Custom;
                    custom.PairingRequested += (sender, args) =>
                    {
                        Debug.WriteLine($"→ Запрошено подтверждение: {args.PairingKind}");
                        args.Accept();
                    };
                    var result = await custom.PairAsync(DevicePairingKinds.ConfirmOnly);
                    Debug.WriteLine($"→ Результат сопряжения: {result.Status}");
                    if (result.Status != DevicePairingResultStatus.Paired)
                        throw new Exception($"Сопряжение не удалось: {result.Status}");
                }

                _connectedDevice = await BluetoothDevice.FromIdAsync(deviceId);
                if (_connectedDevice == null)
                    throw new Exception("Не удалось создать BluetoothDevice.");

                var services = await _connectedDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
                int attempts = 3;
                while (attempts-- > 0 && services.Services.Count == 0)
                {
                    Debug.WriteLine("RFCOMM-сервисы не найдены, повторная попытка...");
                    await Task.Delay(1000);
                    services = await _connectedDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
                }

                _service = services.Services.FirstOrDefault(s => s.ServiceId.Uuid == new Guid("00001101-0000-1000-8000-00805F9B34FB"));
                if (_service == null)
                    throw new Exception("RFCOMM SerialPort не найден.");

                _socket = new StreamSocket();
                Debug.WriteLine($"Попытка подключения к HostName: {_service.ConnectionHostName}, ServiceName: {_service.ConnectionServiceName}");
                await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);

                Debug.WriteLine($"✅ Подключено к {_connectedDevice.Name}");

                return new DeviceStub(_connectedDevice.Name, deviceId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка подключения: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }



        public async Task SendDataAsync(string data)
        {
            try
            {
                if (_socket == null)
                {
                    throw new InvalidOperationException("Bluetooth-соединение не активно");
                }

                var writer = new DataWriter(_socket.OutputStream);
                writer.WriteString(data + "\r\n");
                await writer.StoreAsync();
                writer.DetachStream();
                Debug.WriteLine($"Отправлено: {data}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отправки данных: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectDeviceAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket = null;
                    Debug.WriteLine("Сокет освобожден");
                }
                if (_service != null)
                {
                    _service.Dispose();
                    _service = null;
                    Debug.WriteLine("RFCOMM-сервис освобожден");
                }
                if (_connectedDevice != null)
                {
                    _connectedDevice.Dispose();
                    _connectedDevice = null;
                    Debug.WriteLine("Bluetooth-устройство освобождено");
                }
                Debug.WriteLine("Устройство полностью отключено");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отключения: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void DisconnectDevice()
        {
            Task.Run(DisconnectDeviceAsync).GetAwaiter().GetResult();
        }
    }
}