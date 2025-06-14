using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui;
using Plugin.CurrentActivity;

namespace MirroRehab
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Создание канала уведомлений
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("mirro_rehab_channel", "MirroRehab Service", NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }

            // Запрос на игнорирование оптимизации батареи
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var powerManager = (PowerManager)GetSystemService(PowerService);
                if (!powerManager.IsIgnoringBatteryOptimizations(PackageName))
                {
                    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                    StartActivity(intent);
                }
            }

            RequestBluetoothPermissions();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
        }

        private void RequestBluetoothPermissions()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var permissionsToRequest = new List<string>();

                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                    permissionsToRequest.Add(Manifest.Permission.AccessFineLocation);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothScan) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.BluetoothScan);
                    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothConnect) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.BluetoothConnect);
                }

                if (permissionsToRequest.Count > 0)
                    ActivityCompat.RequestPermissions(this, permissionsToRequest.ToArray(), 0);
            }
        }
    }
}