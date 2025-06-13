using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.OS;
using Android.App;
using Android.Content.PM;
using Plugin.CurrentActivity;

namespace MirroRehab
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("channel_id", "MirroRehab Service", NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager.CreateNotificationChannel(channel);
            }
            RequestBluetoothPermissions();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
        }

        private void RequestBluetoothPermissions()
        {
            // Для Android 6.0+ (API 23+) запрашиваем разрешения во время выполнения
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var permissionsToRequest = new List<string>();

                // Проверяем и запрашиваем необходимые разрешения
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                    permissionsToRequest.Add(Manifest.Permission.AccessFineLocation);

                // Для Android 12+ (API 31+)
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothScan) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.BluetoothScan);
                    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothConnect) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.BluetoothConnect);
                }

                // Запрашиваем разрешения, если они не предоставлены
                if (permissionsToRequest.Count > 0)
                    ActivityCompat.RequestPermissions(this, permissionsToRequest.ToArray(), 0);
            }
        }
    }
}