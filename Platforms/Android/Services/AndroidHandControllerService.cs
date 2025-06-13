using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using MirroRehab.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MirroRehab.Platforms.Android.Services
{
    public class AndroidHandControllerService
    {
        public async Task<bool> StartTracking(CancellationToken cancellationToken, IDevice device)
        {
            try
            {
                if (device == null)
                {
                    System.Diagnostics.Debug.WriteLine("Ошибка: Устройство не выбрано");
                    return false;
                }

                var intent = new Intent(Android.App.Application.Context, typeof(BackgroundService));
                intent.PutExtra("DeviceName", device.Name);
                intent.PutExtra("DeviceAddress", device.Address);
                Android.App.Application.Context.StartForegroundService(intent);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка запуска сервиса на Android: {ex.Message}");
                return false;
            }
        }
    }
}