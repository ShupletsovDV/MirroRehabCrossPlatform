using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MirroRehab.Platforms.Android.Services
{
    [Service]
    public class BackgroundService : Service
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUdpClientService _udpClientService;
        private readonly IBluetoothService _bluetoothService;
        private readonly IPositionProcessor _positionProcessor;
        private CancellationTokenSource _cts;
        private IDevice _device;

        public BackgroundService()
        {
            _serviceProvider = MauiProgram.CreateMauiApp().Services;
            _udpClientService = _serviceProvider.GetRequiredService<IUdpClientService>();
            _bluetoothService = _serviceProvider.GetRequiredService<IBluetoothService>();
            _positionProcessor = _serviceProvider.GetRequiredService<IPositionProcessor>();
            _cts = new CancellationTokenSource();
        }

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var deviceName = intent?.GetStringExtra("DeviceName");
            var deviceAddress = intent?.GetStringExtra("DeviceAddress");

            if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(deviceAddress))
            {
                System.Diagnostics.Debug.WriteLine("[BackgroundService] Ошибка: устройство не передано");
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            _device = new DeviceStub(deviceName, deviceAddress);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelId = "mirro_rehab_channel";
                var channel = new NotificationChannel(channelId, "MirroRehab Service", NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);

                var notification = new Notification.Builder(this, channelId)
                    .SetContentTitle("MirroRehab")
                    .SetContentText($"Прослушивание сервера для устройства {_device.Name}")
                    .SetSmallIcon(Resource.Mipmap.appicon_background)
                    .SetOngoing(true)
                    .Build();

                StartForeground(1, notification);
            }

            Task.Run(() => RunBackgroundTask(_cts.Token));
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Сервис запущен");
            return StartCommandResult.Sticky;
        }

        private async Task RunBackgroundTask(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Фоновая задача запущена");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[BackgroundService] Начало итерации");
                    if (!_bluetoothService.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключение к {_device.Name} ({_device.Address})...");
                        await _bluetoothService.ConnectToDeviceAsync(_device.Address);
                       
                        System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключено к {_device.Name}");
                    }

                    System.Diagnostics.Debug.WriteLine("[BackgroundService] Ожидание данных...");
                    var receiveData = await _udpClientService.ReceiveDataAsync(cancellationToken);
                    if (receiveData != null && receiveData.type == "position")
                    {
                        if (_bluetoothService.IsConnected)
                        {
                            await _positionProcessor.ProcessPositionAsync(receiveData, _bluetoothService);
                            System.Diagnostics.Debug.WriteLine($"[BackgroundService] Данные обработаны: {_device.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[BackgroundService] Соединение потеряно перед отправкой данных");
                            _bluetoothService.DisconnectDevice();
                            continue;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[BackgroundService] Получены некорректные данные или данных нет: {receiveData?.type}");
                    }
                    System.Diagnostics.Debug.WriteLine("[BackgroundService] Итерация завершена");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundService] Ошибка в фоновом сервисе: {ex.Message}, StackTrace: {ex.StackTrace}");
                    _bluetoothService.DisconnectDevice(); // Сброс соединения при ошибке
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        public override void OnDestroy()
        {
            _cts?.Cancel();
            _bluetoothService?.DisconnectDevice();
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Фоновый сервис остановлен");
            base.OnDestroy();
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Сервис удалён из задач");
            base.OnTaskRemoved(rootIntent);
        }

        public override void OnLowMemory()
        {
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Нехватка памяти, сервис может быть остановлен");
            base.OnLowMemory();
        }
    }
}