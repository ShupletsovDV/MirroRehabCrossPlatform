using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;
using Plugin.CurrentActivity;
using System.Diagnostics;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using Java.Util;
using System.Text;

namespace MirroRehab.Platforms
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothAdapter _adapter;
        private BluetoothDevice? _connectedDevice;
        private BluetoothSocket? _socket;

        public BluetoothService()
        {
            _adapter = BluetoothAdapter.DefaultAdapter ?? throw new Exception("Bluetooth-адаптер не найден");
        }

        public bool IsConnected => _connectedDevice != null && _socket != null && _socket.IsConnected;

        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            try
            {
                if (_adapter == null || !_adapter.IsEnabled)
                {
                    throw new InvalidOperationException("Bluetooth отключён");
                }

                var devices = new List<IDevice>();

                // Сопряжённые устройства
                var bondedDevices = _adapter.BondedDevices;
                foreach (var device in bondedDevices ?? Enumerable.Empty<BluetoothDevice>())
                {
                    if (!string.IsNullOrEmpty(device.Name) && device.Name.StartsWith("MirroRehab", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Сопряжено: {device.Name}, Address={device.Address}");
                        devices.Add(new DeviceStub(device.Name, device.Address));
                    }
                }

                // Сканирование несопряжённых устройств
                var discoveredDevices = new List<BluetoothDevice>();
                var receiver = new BluetoothDiscoveryReceiver(device =>
                {
                    if (!string.IsNullOrEmpty(device.Name) &&
                        device.Name.StartsWith("MirroRehab", StringComparison.OrdinalIgnoreCase) &&
                        !discoveredDevices.Any(d => d.Address.Equals(device.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Обнаружено: {device.Name}, Address={device.Address}");
                        discoveredDevices.Add(device);
                    }
                });

                var context = CrossCurrentActivity.CurrentActivity.Current;
                context.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));
                context.RegisterReceiver(receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));

                _adapter.StartDiscovery();
                await Task.Delay(15000); // Сканируем 15 секунд
                _adapter.CancelDiscovery();
                context.UnregisterReceiver(receiver);

                foreach (var device in discoveredDevices)
                {
                    if (!devices.Any(d => string.Equals(device.Name, d.Name))
                    {
                        Devices.Add(new DeviceStub(device.Name, device.Address));
                    }
                }

                return Devices;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска на Android: {ex.Message}");
                throw ex;
            }
        }

        public async Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId)
        {
            try
            {
                if (_adapter == null || !_adapter.IsEnabled)
                {
                    throw new Exception("Bluetooth отключён");
                }

                BluetoothDevice? targetDevice = null;
                int maxRetries = 5;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        // Проверяем сопряжённые устройства
                        var bondedDevices = _adapter.BondedDevices;
                        foreach (var device in bondedDevices ?? Enumerable.Empty<BluetoothDevice>())
                        {
                            if (!string.IsNullOrEmpty(device.Name) &&
                                (device.Name.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) ||
                                 || device.Address.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase)))
                            {
                                targetDevice = device;
                                Debug.WriteLine($"Найдено сопряжено: {device.Name}");
                                break;
                            }
                        }

                        if (targetDevice == null)
                        {
                            // Сканирование
                            var receiver = new BluetoothDiscoveryReceiver(device =>
                            {
                                if (!string.IsNullOrEmpty(device.Name) &&
                                    (device.Name.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) ||
                                     || device.Address.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase)))
                                {
                                    targetDevice = device;
                                    Debug.WriteLine($"Найдено: {device.Name}");
                                }
                            });

                            var context = CrossCurrentActivity.CurrentActivity.Current;
                            context.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));
                            context.RegisterReceiver(receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));

                            _adapter.StartDiscovery();
                            await Task.Delay(15000);
                            _adapter.CancelDiscovery();
                            context.UnregisterReceiver(receiver);
                        }

                        if (targetDevice == null)
                        {
                            throw new Exception($"Устройство '{deviceNameOrId}' не найдено");
                        }

                        _connectedDevice = targetDevice;

                        UUID uuid = UUID.FromString("00001101-0000-0000-8000-00805F9B34FB");
                        _socket = _connectedDevice.CreateRfcommSocketToServiceRecord(uuid);

                        // Проверяем состояние устройства
                        if (_connectedDevice.BondState != Bond.Bonded)
                        {
                            Debug.WriteLine($"Устройство {deviceNameOrId} не сопряжено, пытаемся...");
                            var bondResult = _connectedDevice.CreateBond();
                            if (!bondResult)
                            {
                                throw new Exception("Ошибка сопряжения");
                            }
                            await Task.Delay(2000);
                        }

                        await Task.Run(() => _socket.Connect());
                        Debug.WriteLine($"Подключено к {_deviceConnectedDevice.Name} на попытке {attempt}");
                        return new DeviceStub(_connectedDevice.Name, _connectedDevice.Address);
                    }
                    catch (Java.IO.IOException ex)
                    {
                        Debug.WriteLine($"Ошибка подключения на попытке {attempt}: {ex.Message}, StackTrace: {ex.StackTrace}");
                    }
                    if (_socket != null)
                    {
                        try { _socket.Close(); } catch { }
                        _socket = null;
                    }
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Не удалось подключиться после {maxRetries} попыток: {ex.Message}");
                    }
                    await Task.Delay(500);
                }
            
                

                throw new Exception("Не удалось подключить устройство");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подключения на Android: {ex.Message}");
                throw ex;
            }
        }

        public async Task SendDataAsync(string data)
        {
            try
            {
                if (_socket == null || !_socket?.IsConnected)
                {
                    throw new InvalidOperationException("Bluetooth-соединение не активно");
                }

                byte[] buffer = Encoding.UTF8.GetBytes(data + "\r\n");
                await Task.Run(() => _socket?.OutputStream().Write(buffer, 0, buffer.Length));
                Debug.WriteLine($"Отправлено: {data}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отправки данных: {ex.Message}");
                throw ex;
            }
        }

        public async Task DisconnectDeviceAsync()
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                _connectedDevice = null;
                Debug.WriteLine("Устройство отключено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отключения: {ex.Message}");
                throw ex;
            }
        }

        public void DisconnectDevice()
        {
            Task.Run(() => DisconnectDeviceAsync()).GetAwaiter().GetResult();
        }
    }

    public class BluetoothDiscoveryReceiver : BroadcastReceiver
    {
        private readonly Action<BluetoothDevice> _onDeviceFound;

        public BluetoothDiscoveryReceiver(Action<BluetoothDevice> onDeviceFound)
        {
            _onDeviceFound = onDeviceFound;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent?.Action?.To;

            if (action == BluetoothDevice.ActionFound)
            {
                var device = intent?.GetParcelableExtra(BluetoothDevice.ExtraDeviceDevice) as BluetoothDevice;
                _onDeviceFound?.Invoke(device);
            }
        }
    }
}