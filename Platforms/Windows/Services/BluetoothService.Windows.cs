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

namespace MirroRehab.Platforms.Windows.Services
{
    public class BluetoothService : IBluetoothService
    {
        private BluetoothDevice _connectedDevice;
        private RfcommDeviceService _service;
        private StreamSocket _socket;

        public bool IsConnected => _connectedDevice != null && _socket != null;

        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            try
            {
                var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
                var devices = await DeviceInformation.FindAllAsync(selector);
                var result = new List<IDevice>();

                foreach (var deviceInfo in devices)
                {
                    if (!string.IsNullOrEmpty(deviceInfo.Name) && deviceInfo.Name.StartsWith("MirroRehab", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Найдено: {deviceInfo.Name}, Id: {deviceInfo.Id}");
                        result.Add(new DeviceStub(deviceInfo.Name, deviceInfo.Id));
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска на Windows: {ex.Message}");
                throw;
            }
        }

        public async Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId)
        {
            try
            {
                var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
                var devices = await DeviceInformation.FindAllAsync(selector);
                DeviceInformation targetDeviceInfo = null;

                foreach (var deviceInfo in devices)
                {
                    if (deviceInfo.Name.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase) ||
                        deviceInfo.Id.Equals(deviceNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        targetDeviceInfo = deviceInfo;
                        Debug.WriteLine($"Найдено: {deviceInfo.Name}");
                        break;
                    }
                }

                if (targetDeviceInfo == null)
                {
                    throw new Exception($"Устройство '{deviceNameOrId}' не найдено");
                }

                _connectedDevice = await BluetoothDevice.FromIdAsync(targetDeviceInfo.Id);
                var services = await _connectedDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
                _service = services.Services.FirstOrDefault(s => s.ServiceId == RfcommServiceId.SerialPort);

                if (_service == null)
                    throw new Exception("Служба RFCOMM не найдена");

                _socket = new StreamSocket();
                await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                Debug.WriteLine($"Подключено к {_connectedDevice.Name}");
                return new DeviceStub(_connectedDevice.Name, _connectedDevice.DeviceInformation.Id);
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
            try
            {
                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket = null;
                }
                if (_service != null)
                {
                    _service.Dispose();
                    _service = null;
                }
                _connectedDevice = null;
                Debug.WriteLine("Устройство отключено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отключения: {ex.Message}");
            }
        }

        public void DisconnectDevice()
        {
            Task.Run(DisconnectDeviceAsync).GetAwaiter().GetResult();
        }
    }
}