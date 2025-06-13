using Android.App;
using Android.Content;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using MirroRehab.Interfaces;

namespace MirroRehab.Platforms.Android.Services
{
    public class AndroidHandControllerService
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
                Platform.AppContext.StartForegroundService(intent);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка запуска сервиса на Android: {ex.Message}");
                return false;
            }
        }
    }
}