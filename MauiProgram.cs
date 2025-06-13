using Microsoft.Extensions.Logging;
using MirroRehab.Interfaces;
using MirroRehab.Services;
using MirroRehab.ViewModels;


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

            // Регистрация IBluetoothService
#if ANDROID
            builder.Services.AddSingleton<IBluetoothService, Platforms.Android.Services.BluetoothService>();
#elif WINDOWS
            builder.Services.AddSingleton<IBluetoothService, MirroRehab.Platforms.Windows.BluetoothService>();
#else
            // Для неподдерживаемых платформ (например, iOS, macOS)
            builder.Services.AddSingleton<IBluetoothService>(provider =>
                throw new NotSupportedException("BluetoothService не поддерживается на этой платформе."));
#endif

            builder.Services.AddSingleton<ICalibrationService, CalibrationService>();
            builder.Services.AddSingleton<IUdpClientService, UdpClientService>();
            builder.Services.AddSingleton<IPositionProcessor, PositionProcessor>();
            builder.Services.AddSingleton<HandController>(provider =>
                HandController.GetHandController(
                    provider.GetRequiredService<IBluetoothService>(),
                    provider.GetRequiredService<IUdpClientService>(),
                    provider.GetRequiredService<IPositionProcessor>()));
            builder.Services.AddSingleton<Dictionaries>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
