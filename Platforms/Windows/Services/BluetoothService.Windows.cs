using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;
using MirroRehab.Interfaces;
using Windows.Devices.Radios;

namespace MirroRehab.Platforms.Windows
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothDevice _connectedDevice;
        private StreamSocket _socket;
        private DataWriter _writer;
        private readonly List<DeviceInformation> _discoveredDevices = new List<DeviceInformation>();

        public bool IsConnected => _connectedDevice?.ConnectionStatus == BluetoothConnectionStatus.Connected && _socket != null;

        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            try
            {
                _discoveredDevices.Clear();
                var devices = new List<IDevice>();

                var bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();
                if (bluetoothAdapter == null || !bluetoothAdapter.IsCentralRoleSupported)
                {
                    Debug.WriteLine("Ошибка: Bluetooth-адаптер не найден или не поддерживает центральную роль.");
                    throw new Exception("Bluetooth-адаптер не доступен.");
                }

                var radio = await Radio.GetRadiosAsync();
                var bluetoothRadio = radio.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
                if (bluetoothRadio == null || bluetoothRadio.State != RadioState.On)
                {
                    Debug.WriteLine("Ошибка: Bluetooth выключен или недоступен.");
                    throw new Exception("Bluetooth выключен.");
                }

                Debug.WriteLine("Начинаем поиск несопряжённых устройств...");
                var unpairedSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(false);
                var unpairedDevices = await DeviceInformation.FindAllAsync(unpairedSelector);
                foreach (var deviceInfo in unpairedDevices)
                {
                    Debug.WriteLine($"Найдено несопряжённое устройство: Name={deviceInfo.Name ?? "Unknown"}, ID={deviceInfo.Id}");
                    if (!string.IsNullOrEmpty(deviceInfo.Name) && deviceInfo.Name.StartsWith("Mirro", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (_discoveredDevices)
                        {
                            if (!_discoveredDevices.Any(d => d.Id == deviceInfo.Id))
                            {
                                Debug.WriteLine($"Добавлено несопряжённое устройство Mirro: Name={deviceInfo.Name}, ID={deviceInfo.Id}");
                                _discoveredDevices.Add(deviceInfo);
                            }
                        }
                    }
                }

                Debug.WriteLine("Начинаем поиск сопряжённых устройств...");
                var pairedSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
                var pairedDevices = await DeviceInformation.FindAllAsync(pairedSelector);
                foreach (var deviceInfo in pairedDevices)
                {
                    Debug.WriteLine($"Найдено сопряжённое устройство: Name={deviceInfo.Name ?? "Unknown"}, ID={deviceInfo.Id}");
                    if (!string.IsNullOrEmpty(deviceInfo.Name) && deviceInfo.Name.StartsWith("Mirro", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (_discoveredDevices)
                        {
                            if (!_discoveredDevices.Any(d => d.Id == deviceInfo.Id))
                            {
                                Debug.WriteLine($"Добавлено сопряжённое устройство Mirro: Name={deviceInfo.Name}, ID={deviceInfo.Id}");
                                _discoveredDevices.Add(deviceInfo);
                            }
                        }
                    }
                }

                Debug.WriteLine($"Поиск завершён. Найдено устройств: {_discoveredDevices.Count}");

                foreach (var deviceInfo in _discoveredDevices)
                {
                    try
                    {
                        var device = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                        if (device == null)
                        {
                            Debug.WriteLine($"Не удалось получить BluetoothDevice для {deviceInfo.Name}");
                            continue;
                        }

                        devices.Add(new BluetoothDeviceWrapper(device));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка обработки устройства {deviceInfo.Name}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"Найдено устройств Mirro: {devices.Count}");
                return devices;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска устройств на Windows: {ex.Message}");
                throw;
            }
        }

        public async Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId)
        {
            try
            {
                // Проверяем, подключено ли устройство
                if (IsConnected && (_connectedDevice?.Name?.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) == true ||
                    _connectedDevice?.DeviceInformation.Id.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) == true))
                {
                    Debug.WriteLine($"Устройство {deviceNameOrId} уже подключено.");
                    return new BluetoothDeviceWrapper(_connectedDevice);
                }

                // Отключаем только если подключаемся к другому устройству
                if (_connectedDevice != null)
                {
                    await DisconnectDeviceAsync();
                }

                DeviceInformation targetDevice;
                lock (_discoveredDevices)
                {
                    targetDevice = _discoveredDevices.FirstOrDefault(d =>
                        d.Name.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) ||
                        d.Id.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase));
                }

                if (targetDevice == null)
                {
                    var selector = BluetoothDevice.GetDeviceSelector();
                    var devices = await DeviceInformation.FindAllAsync(selector);
                    targetDevice = devices.FirstOrDefault(d =>
                        d.Name.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) ||
                        d.Id.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase));

                    if (targetDevice == null)
                    {
                        Debug.WriteLine($"Устройство '{deviceNameOrId}' не найдено");
                        throw new Exception($"Устройство '{deviceNameOrId}' не найдено");
                    }
                    lock (_discoveredDevices)
                    {
                        _discoveredDevices.Add(targetDevice);
                    }
                }

                _connectedDevice = await BluetoothDevice.FromIdAsync(targetDevice.Id);
                if (_connectedDevice == null)
                {
                    Debug.WriteLine($"Не удалось получить устройство: {targetDevice.Name}");
                    throw new Exception($"Не удалось получить устройство: {targetDevice.Name}");
                }

                // Проверяем сопряжение
                if (!targetDevice.Pairing.IsPaired)
                {
                    Debug.WriteLine($"Инициируем сопряжение с {targetDevice.Name}");
                    var customPairing = targetDevice.Pairing.Custom;
                    customPairing.PairingRequested += (sender, args) =>
                    {
                        Debug.WriteLine($"Тип сопряжения: {args.PairingKind}. Подтверждаем сопряжение.");
                        args.Accept();
                    };
                    var pairingResult = await customPairing.PairAsync(DevicePairingKinds.ConfirmOnly);
                    if (pairingResult.Status != DevicePairingResultStatus.Paired && pairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
                    {
                        Debug.WriteLine($"Ошибка сопряжения с {targetDevice.Name}: {pairingResult.Status}");
                        throw new Exception($"Ошибка сопряжения: {pairingResult.Status}");
                    }
                    Debug.WriteLine($"Сопряжение успешно: {targetDevice.Name}");
                    await Task.Delay(3000); // Увеличили задержку
                }
                else
                {
                    Debug.WriteLine($"Устройство {targetDevice.Name} уже сопряжено.");
                }

                // Получаем RFCOMM-сервисы
                var services = await _connectedDevice.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort, BluetoothCacheMode.Uncached);
                Debug.WriteLine($"Найдено RFCOMM-сервисов: {services.Services.Count}");
                foreach (var svc in services.Services)
                {
                    Debug.WriteLine($"Сервис: UUID={svc.ServiceId.Uuid}, HostName={svc.ConnectionHostName}, ServiceName={svc.ConnectionServiceName}");
                }

                var service = services.Services.FirstOrDefault();
                if (service == null)
                {
                    Debug.WriteLine("RFCOMM-сервис не найден для UUID 00001101-0000-1000-8000-00805f9b34fb");
                    throw new Exception("RFCOMM-сервис не найден");
                }

                // Попытки подключения
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        Debug.WriteLine($"Попытка подключения {attempt} к {deviceNameOrId} с UUID {service.ServiceId.Uuid}");
                        _socket = new StreamSocket();
                        await _socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName, SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                        _writer = new DataWriter(_socket.OutputStream);
                        Debug.WriteLine($"Подключено к {_connectedDevice.Name} на попытке {attempt}");
                        return new BluetoothDeviceWrapper(_connectedDevice);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка подключения на попытке {attempt}: {ex.Message} (HResult: {ex.HResult})");
                        if (_socket != null)
                        {
                            _socket.Dispose();
                            _socket = null;
                        }
                        if (attempt == 3)
                        {
                            throw new Exception($"Не удалось подключиться после 3 попыток: {ex.Message}");
                        }
                        await Task.Delay(2000);
                    }
                }

                throw new Exception("RFCOMM-сервис не найден после всех попыток");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подключения на Windows: {ex.Message}");
                throw;
            }
        }

        public async Task SendDataAsync(string data)
        {
            try
            {
                if (!IsConnected || _writer == null || _socket == null)
                {
                    Debug.WriteLine("Bluetooth-соединение не инициализировано");
                    throw new InvalidOperationException("Bluetooth-соединение не инициализировано");
                }

                _writer.WriteString(data + "\r\n");
                await _writer.StoreAsync();
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
            try
            {
                if (_writer != null)
                {
                    _writer.DetachStream();
                    _writer = null;
                }
                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket = null;
                }
                if (_connectedDevice != null)
                {
                    _connectedDevice.Dispose();
                    Debug.WriteLine("Устройство отключено");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отключения: {ex.Message}");
            }
            finally
            {
                _connectedDevice = null;
            }
        }

        public void DisconnectDevice()
        {
            Task.Run(DisconnectDeviceAsync).Wait();
        }
    }

    public class BluetoothDeviceWrapper : IDevice
    {
        private readonly BluetoothDevice _device;

        public BluetoothDeviceWrapper(BluetoothDevice device)
        {
            _device = device;
        }

        public string Name => _device.Name ?? "Unknown";
        public string Address => _device.DeviceInformation.Id;
        public Guid Id => Guid.NewGuid();
        public DeviceState State => _device.ConnectionStatus == BluetoothConnectionStatus.Connected ? DeviceState.Connected : DeviceState.Disconnected;
    }
}