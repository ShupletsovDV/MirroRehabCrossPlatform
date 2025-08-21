using Android.App;
using Android.Content;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Android.Services
{
    public class AndroidHandControllerService : IHandController
    {
        public async Task<bool> StartTrackingAsync(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не найдено");
                    return false;
                }

                var intent = new Intent(Platform.AppContext, Java.Lang.Class.FromType(typeof(BackgroundService)));
                intent.PutExtra("DeviceName", device.Name);
                intent.PutExtra("DeviceAddress", device.Address);
                intent.PutExtra("ActionType", "tracking");
                Platform.AppContext.StartForegroundService(intent);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка запуска сервиса на Android: {ex.Message}");
                return false;
            }
        }

        public Task<bool> CalibrateDevice(CancellationToken cancellationToken,IDevice device)
        {
            throw new NotSupportedException("Калибровка должна выполняться через основной HandController.");
        }

        public async Task DemoMirro(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    Debug.WriteLine("Ошибка: Устройство не найдено");
                    return;
                }

                // Создаем Intent для запуска BackgroundService с необходимыми параметрами
                var intent = new Intent(Platform.AppContext, Java.Lang.Class.FromType(typeof(BackgroundService)));
                intent.PutExtra("DeviceName", device.Name);
                intent.PutExtra("DeviceAddress", device.Address);
                intent.PutExtra("ActionType", "demo");

                // Запускаем BackgroundService в фоновом режиме
                Platform.AppContext.StartForegroundService(intent);

                Debug.WriteLine("Демо успешно запущено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка запуска демо-сессии на Android: {ex.Message}");
            }
        }
    }
}