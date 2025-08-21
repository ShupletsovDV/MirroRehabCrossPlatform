using Android.App;
using Android.Content;
using Android.OS;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using MirroRehab.Services;

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
            var actionType = intent?.GetStringExtra("ActionType");

            if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(deviceAddress))
            {
                System.Diagnostics.Debug.WriteLine("[BackgroundService] Ошибка: устройство не передано");
                StopSelf();
                return StartCommandResult.NotSticky;  // Сервис не перезапускается
            }

            _device = new DeviceStub(deviceName, deviceAddress);

            // Запуск Notification для Foreground
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelId = "mirro_rehab_channel";
                var channel = new NotificationChannel(channelId, "MirroRehab Service", NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);

                var notification = new Notification.Builder(this, channelId)
                    .SetContentTitle("MirroRehab")
                    .SetContentText($"Прослушивание сервера для устройства {_device.Name}")
                    .SetSmallIcon(Resource.Mipmap.appicon)
                    .SetOngoing(true)
                    .Build();

                StartForeground(1, notification);
            }

            // В зависимости от ActionType выполняем отслеживание или демо
            if (actionType == "tracking")
            {
                Task.Run(() => RunTrackingTask(_cts.Token));
            }
            else if (actionType == "demo")
            {
                Task.Run(() => RunDemoTask(_cts.Token));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BackgroundService] Неизвестный тип действия");
                StopSelf();  // Остановка сервиса, если тип действия неизвестен
            }

            System.Diagnostics.Debug.WriteLine("[BackgroundService] Сервис запущен");
            return StartCommandResult.NotSticky;  // Останавливаем сервис после выполнения задачи
        }

        private async Task RunTrackingTask(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Запуск отслеживания");

            try
            {
                if (!_bluetoothService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключение к {_device.Name} ({_device.Address})...");
                    await _bluetoothService.ConnectToDeviceAsync(_device.Address);
                    System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключено к {_device.Name}");
                }

                // Логика отслеживания
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Реализация отслеживания...
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BackgroundService] Ошибка отслеживания: {ex.Message}");
            }
            finally
            {
                _bluetoothService.DisconnectDevice();
                System.Diagnostics.Debug.WriteLine("[BackgroundService] Завершение отслеживания");
                StopSelf(); // Остановка сервиса после завершения отслеживания
            }
        }

        private async Task RunDemoTask(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[BackgroundService] Запуск демо");

            try
            {
                if (!_bluetoothService.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключение к {_device.Name} ({_device.Address})...");
                    await _bluetoothService.ConnectToDeviceAsync(_device.Address);
                    System.Diagnostics.Debug.WriteLine($"[BackgroundService] Подключено к {_device.Name}");
                }

                // Демо-запуск
                
                await HandController.DemoMirro(cancellationToken, _device,_bluetoothService,new Dictionaries());

                System.Diagnostics.Debug.WriteLine("[BackgroundService] Демо завершено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BackgroundService] Ошибка выполнения демо: {ex.Message}");
            }
            finally
            {
                _bluetoothService.DisconnectDevice();
                System.Diagnostics.Debug.WriteLine("[BackgroundService] Завершение демо");
                StopSelf(); // Остановка сервиса после завершения демо
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
