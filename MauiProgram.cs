using Microsoft.Extensions.Logging;
using MirroRehab.Interfaces;
using MirroRehab.Services;
using MirroRehab.ViewModels;
using MirroRehab;


namespace MirroRehab
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Регистрация сервисов
            builder.Services.AddSingleton<IHandController, HandController>();
            builder.Services.AddSingleton<ICalibrationService, CalibrationService>();
            builder.Services.AddSingleton<IUdpClientService, UdpClientService>();
            builder.Services.AddSingleton<IPositionProcessor, PositionProcessor>();
            builder.Services.AddSingleton<Dictionaries>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddSingleton<AppShell>();

            // Платформоспецифичные сервисы
#if ANDROID
            builder.Services.AddSingleton<IBluetoothService, MirroRehab.Platforms.Android.Services.BluetoothService>();
#elif WINDOWS
            builder.Services.AddSingleton<IBluetoothService, MirroRehab.Platforms.Windows.Services.BluetoothService>();
#elif IOS || MACCATALYST
            builder.Services.AddSingleton<IBluetoothService>(sp => throw new PlatformNotSupportedException("Bluetooth не поддерживается на iOS и macCatalyst"));
#else
            builder.Services.AddSingleton<IBluetoothService>(sp => throw new PlatformNotSupportedException("Платформа не поддерживается"));
#endif

            return builder.Build();
        }
    }
}