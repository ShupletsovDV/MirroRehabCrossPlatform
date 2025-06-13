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
        private readonly IUdpClientService _udpClientService;
        private readonly IBluetoothService _bluetoothService;
        private readonly IPositionProcessor _positionProcessor;
        private CancellationTokenSource _cts;
        private IDevice _device;

        public BackgroundService()
        {
            var serviceProvider = MauiProgram.CreateMauiApp().Services;
            _udpClientService = serviceProvider.GetRequiredService<IUdpClientService>();
            _bluetoothService = serviceProvider.GetRequiredService<IBluetoothService>();
            _positionProcessor = serviceProvider.GetRequiredService<IPositionProcessor>();
            _cts = new CancellationTokenSource();
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var deviceName = intent?.GetStringExtra("DeviceName");
            var deviceAddress = intent?.GetStringExtra("DeviceAddress");

            if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(deviceAddress))
            {
                System.Diagnostics.Debug.WriteLine("Ошибка: устройство не передано");
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            _device = new DeviceStub(deviceName, deviceAddress);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("mirro_rehab_channel", "MirroRehab Service", NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);

                var notification = new Notification.Builder(this, "mirro_rehab_channel")
                    .SetContentTitle("MirroRehab")
                    .SetContentText($"Прослушивание сервера для устройства {_device.Name}")
                    .SetSmallIcon(Resource.Drawable.ic_notification)
                    .Build();

                StartForeground(1, notification);
            }

            Task.Run(() => RunBackgroundTask(_cts.Token));
            return StartCommandResult.Sticky;
        }

        private async Task RunBackgroundTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_bluetoothService.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine($"Подключение к {_device.Name} ({_device.Address})...");
                        await _bluetoothService.ConnectToDeviceAsync(_device.Address);
                    }

                    await _udpClientService.StartPingAsync();
                    var receiveData = await _udpClientService.ReceiveDataAsync();
                    if (receiveData != null && receiveData.type == "position")
                    {
                        await _positionProcessor.ProcessPositionAsync(receiveData, _bluetoothService);
                        System.Diagnostics.Debug.WriteLine($"Данные обработаны и отправлены на устройство: {_device.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Получены некорректные данные или данных нет");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка в фоновом сервисе: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        public override void OnDestroy()
        {
            _cts?.Cancel();
            _bluetoothService?.DisconnectDeviceAsync().GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("Фоновый сервис остановлен");
            base.OnDestroy();
        }
    }
}