using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Android.Bluetooth;
using Android.Content;

using Java.Util;
using Plugin.CurrentActivity;

using MirroRehab.Interfaces;
using MirroRehab.Models;

namespace MirroRehab.Platforms.Android.Services
{
    public class BluetoothService : IBluetoothService
    {
        private readonly BluetoothAdapter _adapter;
        private BluetoothDevice? _connectedDevice;
        private BluetoothSocket? _socket;

        public BluetoothService()
        {
            _adapter = BluetoothAdapter.DefaultAdapter ?? throw new Exception("Bluetooth-адаптер не найден");
        }

        public bool IsConnected => _connectedDevice != null && _socket != null && _socket.IsConnected;

        // -------------------------
        // DISCOVERY (с опц. сопряжением)
        // -------------------------
        public async Task<List<IDevice>> DiscoverMirroRehabDevicesAsync()
        {
            bool pairIfNeeded = true;
            if (_adapter == null || !_adapter.IsEnabled)
                throw new InvalidOperationException("Bluetooth отключён");

            var context = CrossCurrentActivity.Current?.Activity
                ?? throw new InvalidOperationException("Нет активного контекста Activity");

            var result = new List<IDevice>();
            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Спарённые устройства
            foreach (var dev in _adapter.BondedDevices ?? Enumerable.Empty<BluetoothDevice>())
            {
                var name = dev?.Name;
                var addr = dev?.Address;

                if (!string.IsNullOrEmpty(name) &&
                    name.StartsWith("MirroRehab", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(addr) &&
                    seenAddresses.Add(addr))
                {
                    Debug.WriteLine($"[Discover] Paired: {name} ({addr})");

                    if (pairIfNeeded && dev!.BondState != Bond.Bonded)
                        await EnsureBondAsync(dev!, TimeSpan.FromSeconds(20)); // на всякий

                    result.Add(new DeviceStub(name!, addr!));
                }
            }

            // 2) Discovery не спарённых
            var discovered = new List<BluetoothDevice>();
            var receiver = new BluetoothDiscoveryReceiver(dev =>
            {
                if (dev is null) return;
                var name = dev.Name;
                var addr = dev.Address;

                if (!string.IsNullOrEmpty(name) &&
                    name.StartsWith("MirroRehab", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(addr) &&
                    !seenAddresses.Contains(addr))
                {
                    seenAddresses.Add(addr);
                    discovered.Add(dev);
                    Debug.WriteLine($"[Discover] Found: {name} ({addr})");
                }
            });

            try
            {
                context.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));
                context.RegisterReceiver(receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));

                if (_adapter.IsDiscovering) _adapter.CancelDiscovery();
                _adapter.StartDiscovery();

                // Ждём 12–15 секунд — обычно хватает, чтобы найти ближние девайсы
                await Task.Delay(TimeSpan.FromSeconds(12));
            }
            finally
            {
                try { _adapter.CancelDiscovery(); } catch { }
                try { context.UnregisterReceiver(receiver); } catch { }
            }

            // Добавляем найденные
            foreach (var dev in discovered)
            {
                var name = dev?.Name ?? "(no name)";
                var addr = dev?.Address;
                if (!string.IsNullOrEmpty(addr))
                {
                    if (pairIfNeeded && dev!.BondState != Bond.Bonded)
                        await EnsureBondAsync(dev!, TimeSpan.FromSeconds(20));

                    result.Add(new DeviceStub(name, addr));
                }
            }

            return result;
        }

        // -------------------------
        // CONNECT (по имени или адресу)
        // -------------------------
        public async Task<IDevice> ConnectToDeviceAsync(string deviceNameOrId)
        {
            if (_adapter == null || !_adapter.IsEnabled)
                throw new InvalidOperationException("Bluetooth отключён");

            var context = CrossCurrentActivity.Current?.Activity
                ?? throw new InvalidOperationException("Нет активного контекста Activity");

            // 1) Ищем среди спарённых
            BluetoothDevice? target = null;
            foreach (var dev in _adapter.BondedDevices ?? Enumerable.Empty<BluetoothDevice>())
            {
                if (Matches(dev, deviceNameOrId))
                {
                    target = dev;
                    Debug.WriteLine($"[Connect] Found paired: {dev.Name} ({dev.Address})");
                    break;
                }
            }

            // 2) Если не нашли — слушаем discovery с ранним выходом
            if (target is null)
            {
                var tcs = new TaskCompletionSource<BluetoothDevice?>();
                var receiver = new BluetoothDiscoveryReceiver(dev =>
                {
                    if (dev is null) return;
                    if (Matches(dev, deviceNameOrId) && !tcs.Task.IsCompleted)
                    {
                        tcs.TrySetResult(dev);
                    }
                });

                try
                {
                    context.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));
                    context.RegisterReceiver(receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));

                    if (_adapter.IsDiscovering) _adapter.CancelDiscovery();
                    _adapter.StartDiscovery();

                    var foundTask = tcs.Task;
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
                    var completed = await Task.WhenAny(foundTask, timeoutTask);

                    if (completed == foundTask)
                    {
                        target = foundTask.Result;
                        if (target != null)
                            Debug.WriteLine($"[Connect] Discovered: {target.Name} ({target.Address})");
                    }
                }
                finally
                {
                    try { _adapter.CancelDiscovery(); } catch { }
                    try { context.UnregisterReceiver(receiver); } catch { }
                }
            }

            if (target is null)
                throw new Exception($"Устройство '{deviceNameOrId}' не найдено");

            // 3) Сопряжение при необходимости
            if (target.BondState != Bond.Bonded)
            {
                Debug.WriteLine($"[Connect] Pairing with {target.Name}...");
                var ok = await EnsureBondAsync(target, TimeSpan.FromSeconds(20));
                if (!ok) throw new Exception("Не удалось выполнить сопряжение");
            }

            // 4) Подключение (SPP). Discovery должен быть остановлен
            _connectedDevice = target;

            const int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (_adapter.IsDiscovering) _adapter.CancelDiscovery();

                    var uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"); // SPP
                    _socket = _connectedDevice.CreateRfcommSocketToServiceRecord(uuid);

                    await Task.Run(() => _socket.Connect());
                    Debug.WriteLine($"[Connect] Connected to {_connectedDevice.Name} on attempt #{attempt}");

                    return new DeviceStub(_connectedDevice.Name ?? "(no name)", _connectedDevice.Address ?? "");
                }
                catch (Java.IO.IOException ex)
                {
                    Debug.WriteLine($"[Connect] Attempt {attempt} failed: {ex.Message}");
                    try { _socket?.Close(); } catch { }
                    _socket = null;

                    if (attempt == maxRetries)
                        throw new Exception($"Не удалось подключиться: {ex.Message}");

                    await Task.Delay(500);
                }
            }

            throw new Exception("Не удалось подключить устройство");
        }

        // -------------------------
        // SEND
        // -------------------------
        public async Task SendDataAsync(string data)
        {
            try
            {
                // Проверка, если сокет не подключен или закрыт, пытаемся повторно подключиться
                if (_socket == null || !_socket.IsConnected)
                {
                    Debug.WriteLine("[SendData] Соединение не активно, пытаемся переподключиться...");
                    await TryReconnectAsync();
                }

                if (_socket == null || !_socket.IsConnected)
                {
                    throw new InvalidOperationException("Bluetooth-соединение не активно");
                }

                var payload = Encoding.UTF8.GetBytes(data + "\r\n");
                await Task.Run(() => _socket.OutputStream.Write(payload, 0, payload.Length));
                Debug.WriteLine($"[Send] {data}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendData] Ошибка отправки данных: {ex.Message}");
                throw;
            }
        }

        private async Task TryReconnectAsync()
        {
            try
            {
                if (_connectedDevice == null)
                {
                    Debug.WriteLine("[TryReconnect] Устройство не найдено для переподключения");
                    return;
                }

                // Пытаемся переподключить устройство
                Debug.WriteLine("[TryReconnect] Переподключение...");
                await ConnectToDeviceAsync(_connectedDevice.Address);

                // Убедитесь, что сокет теперь подключен
                if (_socket != null && _socket.IsConnected)
                {
                    Debug.WriteLine("[TryReconnect] Устройство успешно переподключено");
                }
                else
                {
                    Debug.WriteLine("[TryReconnect] Не удалось переподключить устройство");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TryReconnect] Ошибка переподключения: {ex.Message}");
            }
        }

        // -------------------------
        // DISCONNECT
        // -------------------------
        public async Task DisconnectDeviceAsync()
        {
            try
            {
                if (_socket != null)
                {
                    try { _socket.Close(); } catch { }
                    _socket = null;
                }
                _connectedDevice = null;
                Debug.WriteLine("[Disconnect] Disconnected");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Disconnect] Error: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        public void DisconnectDevice()
        {
            Task.Run(DisconnectDeviceAsync).GetAwaiter().GetResult();
        }

        // -------------------------
        // HELPERS
        // -------------------------
        private static bool Matches(BluetoothDevice? dev, string? query)
        {
            if (dev is null || string.IsNullOrEmpty(query)) return false;

            // Сверяем адрес без двоеточий (надежнее)
            var addr = dev.Address;
            if (!string.IsNullOrEmpty(addr))
            {
                var normAddr = addr.Replace(":", "").ToUpperInvariant();
                var normQuery = query.Replace(":", "").ToUpperInvariant();
                if (normAddr == normQuery) return true;
            }

            var name = dev.Name;
            if (!string.IsNullOrEmpty(name))
                return string.Equals(name, query, StringComparison.OrdinalIgnoreCase);

            return false;
        }

        private Task<bool> EnsureBondAsync(BluetoothDevice device, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<bool>();
            var context = CrossCurrentActivity.Current?.Activity
                ?? throw new InvalidOperationException("Нет активного контекста Activity");

            BroadcastReceiver? receiver = null;
            receiver = new BondStateReceiver(changed =>
            {
                if (changed?.Address == device.Address)
                {
                    if (changed.BondState == Bond.Bonded)
                        tcs.TrySetResult(true);
                    else if (changed.BondState == Bond.None)
                        tcs.TrySetResult(false);
                }
            });

            try
            {
                context.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionBondStateChanged));

                // Запускаем bond
                if (!device.CreateBond())
                    tcs.TrySetResult(false);

                return Task.WhenAny(tcs.Task, Task.Delay(timeout))
                           .ContinueWith(t => t.Result == tcs.Task && tcs.Task.Result);
            }
            finally
            {
                // слегка отложим снятие ресивера, чтобы гарантированно не ловить race
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    try { context.UnregisterReceiver(receiver); } catch { }
                });
            }
        }
    }

    // РЕСИВЕР: обнаружение устройств
    public class BluetoothDiscoveryReceiver : BroadcastReceiver
    {
        private readonly Action<BluetoothDevice?> _onDeviceFound;

        public BluetoothDiscoveryReceiver(Action<BluetoothDevice?> onDeviceFound) =>
            _onDeviceFound = onDeviceFound;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == BluetoothDevice.ActionFound)
            {
                var dev = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                _onDeviceFound?.Invoke(dev);
            }
        }
    }

    // РЕСИВЕР: изменение состояния bond’а (сопряжения)
    public class BondStateReceiver : BroadcastReceiver
    {
        private readonly Action<BluetoothDevice?> _onBondChanged;

        public BondStateReceiver(Action<BluetoothDevice?> onBondChanged) =>
            _onBondChanged = onBondChanged;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == BluetoothDevice.ActionBondStateChanged)
            {
                var dev = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                _onBondChanged?.Invoke(dev);
            }
        }
    }
}
