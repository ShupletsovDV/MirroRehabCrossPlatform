using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using MirroRehab.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

#if ANDROID
using Android.Content;
#endif
using Microsoft.Maui.ApplicationModel;

namespace MirroRehab.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IHandController handController;
        private IBluetoothService bluetoothService;
        private CancellationTokenSource _cancellationTokenSource;
        private IDevice _connectedDevice;

        [ObservableProperty]
        private string messageInfo = "Поиск устройства...";

        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        private bool isCalibrated;

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool showError;

        [ObservableProperty]
        private Color statusColor = Colors.Black;

        [ObservableProperty]
        private bool isCalibrating;

        [ObservableProperty]
        private ObservableCollection<IDevice> devices = new ObservableCollection<IDevice>();

        [ObservableProperty]
        private IDevice selectedDevice;

        public bool IsNotBusy => !IsBusy;


        #region calibrate
        private const string CalibrationPrefsKey = "MirroRehabCalibrationV1";

        [ObservableProperty] double thumbFlex;
        [ObservableProperty] double thumbExtend;
        [ObservableProperty] double indexFlex;
        [ObservableProperty] double indexExtend;
        [ObservableProperty] double middleFlex;
        [ObservableProperty] double middleExtend;
        [ObservableProperty] double ringFlex;
        [ObservableProperty] double ringExtend;
        [ObservableProperty] double pinkyFlex;
        [ObservableProperty] double pinkyExtend;
        #endregion

        public MainPageViewModel(IBluetoothService btService, IHandController controller)
        {
            IsBusy = true;

            bluetoothService = btService;
            handController = controller;

            Application.Current.UserAppTheme = AppTheme.Light;
            //handController.TrackingDataReceived += OnTrackingDataReceived;

            LoadSavedDevices();
            //Task.Run(InitialSearchDevicesAsync);
        }
        private void OnTrackingDataReceived(object sender, string data)
        {
            MessageInfo = data; // Обновление UI
            Debug.WriteLine($"[MainPageViewModel] Получены данные: {data}");
        }
        private void LoadSavedDevices()
        {
            var savedDevices = Preferences.Get("MirroRehabDevices", string.Empty);
            if (!string.IsNullOrEmpty(savedDevices))
            {
                var deviceList = savedDevices.Split(';').Where(s => !string.IsNullOrEmpty(s)).Select(s =>
                {
                    var parts = s.Split('|');
                    return new DeviceStub(parts[0], parts[1]);
                }).Cast<IDevice>().ToList();
                foreach (var device in deviceList)
                {
                    Devices.Add(device);
                }
            }
        }

        private void SaveDevices()
        {
            var deviceString = string.Join(";", Devices.Select(d => $"{d.Name}|{d.Address}"));
            Preferences.Set("MirroRehabDevices", deviceString);
        }

        private async Task InitialSearchDevicesAsync()
        {
            await SearchDevices();
            IsBusy = false;
        }

        [RelayCommand]
        private async Task SearchDevices()
        {
            try
            {
                IsBusy = true;
                MessageInfo = "Поиск устройств MirroRehab...";
                ShowError = false;

               
                var discoveredDevices = await bluetoothService.DiscoverMirroRehabDevicesAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Devices.Clear();
                    foreach (var device in discoveredDevices)
                    {
                        Devices.Add(device);
                    }
                    MessageInfo = discoveredDevices.Any() ? "Устройства найдены" : "Устройства не найдены";
                });

                SaveDevices();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MessageInfo = $"Ошибка поиска: {ex.Message}";
                    ShowError = true;
                    StatusColor = Colors.Red;
                });
                Debug.WriteLine($"Ошибка поиска устройств: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EnsureConnected()
        {
            
            if (SelectedDevice == null)
            {
                MessageInfo = "Выберите устройство";
                StatusColor = Colors.Yellow;
                throw new InvalidOperationException("Устройство не выбрано");
            }

            if (_connectedDevice == null || !bluetoothService.IsConnected || _connectedDevice.Address != SelectedDevice.Address)
            {
                Debug.WriteLine($"Подключение к {SelectedDevice.Name}...");
                try
                {
                    _connectedDevice = await bluetoothService.ConnectToDeviceAsync(SelectedDevice.Address);
                    IsConnected = bluetoothService.IsConnected;
                    MessageInfo = $"Подключено к {SelectedDevice.Name}";
                    StatusColor = Colors.Green;
                }
                catch (Exception ex)
                {
                    await bluetoothService.DisconnectDeviceAsync();
                    throw;
                }
            }
        }

        [RelayCommand]
        private async Task CalibrateDevice()
        {
            if (SelectedDevice == null)
            {
                MessageInfo = "Выберите устройство для калибровки";
                ShowError = true;
                StatusColor = Colors.Red;
                return;
            }

            try
            {
                IsBusy = true;
                IsCalibrating = true;
                MessageInfo = $"Калибровка устройства {SelectedDevice.Name}...";

               /* var success = await handController.CalibrateDevice(_cancellationTokenSource.Token,SelectedDevice);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsCalibrated = success;
                    MessageInfo = success ? "Калибровка успешна" : "Ошибка калибровки";
                    StatusColor = success ? Colors.Green : Colors.Red;
                    ShowError = !success;
                });*/
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MessageInfo = $"Ошибка калибровки: {ex.Message}";
                    ShowError = true;
                    StatusColor = Colors.Red;
                });
                Debug.WriteLine($"Ошибка калибровки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
               
            }
        }

        [RelayCommand]
        private async Task StartTracking()
        {
            try
            {
                /*if (IsBusy || !IsCalibrated) return;
                IsBusy = true;*/
                await EnsureConnected();
                _cancellationTokenSource = new CancellationTokenSource();
                IsRunning = true;
                IsCalibrated = true;
                Debug.WriteLine($"Запуск отслеживания...");

                Task.Run(async()=>await handController.StartTrackingAsync(_cancellationTokenSource.Token, SelectedDevice));

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отслеживания: {ex.Message}");
                MessageInfo = $"Ошибка отслеживания: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsRunning = false;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void StopTracking()
        {

            _cancellationTokenSource?.Cancel();
            bluetoothService.DisconnectDevice();
            IsConnected = false;
            IsRunning = false;
            IsCalibrated = false;
            _connectedDevice = null;
            MessageInfo = "Отслеживание остановлено.";
            StatusColor = Colors.Black;
        }

        [RelayCommand]
        private async Task DemoMirroAsync()
        {
            try
            {

                if (IsBusy) return;
                if (SelectedDevice == null)
                {
                    MessageInfo = "Выберите устройство";
                    StatusColor = Colors.Red;
                    return;
                }
                IsBusy = true;
                await EnsureConnected();
                _cancellationTokenSource = new CancellationTokenSource();
                IsRunning = true;
                MessageInfo = "Запуск демо...";
                StatusColor = Colors.Black;

                await handController.DemoMirro(_cancellationTokenSource.Token, SelectedDevice);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в демо: {ex.Message}");
                MessageInfo = $"Ошибка демо: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
                IsRunning = false;
            }
        }



        #region calibrate
        partial void OnIsCalibratingChanged(bool value)
        {
            // При открытии подтянем сохранённые значения
            if (value) LoadCalibration();
        }

        [RelayCommand]
        private void OpenCalibration() => IsCalibrating = true;

        [RelayCommand]
        private void CancelCalibration()
        {
            IsCalibrating = false;
            MessageInfo = "Калибровка отменена.";
            StatusColor = Colors.Black;
        }

        [RelayCommand]
        private async Task SaveCalibration()
        {
            try
            {
                var data = new CalibrationData
                {
                    ThumbFlex = ThumbFlex,
                    ThumbExtend = ThumbExtend,
                    IndexFlex = IndexFlex,
                    IndexExtend = IndexExtend,
                    MiddleFlex = MiddleFlex,
                    MiddleExtend = MiddleExtend,
                    RingFlex = RingFlex,
                    RingExtend = RingExtend,
                    PinkyFlex = PinkyFlex,
                    PinkyExtend = PinkyExtend
                };

                var json = JsonSerializer.Serialize(data);
                Preferences.Set(CalibrationPrefsKey, json);

                // По желанию: сразу отправить в прошивку (пример)
                // if (SelectedDevice != null && bluetoothService.IsConnected)
                // {
                //     string payload = $"CAL:{ThumbFlex},{ThumbExtend},{IndexFlex},{IndexExtend},{MiddleFlex},{MiddleExtend},{RingFlex},{RingExtend},{PinkyFlex},{PinkyExtend}";
                //     await bluetoothService.SendDataAsync(payload);
                // }

                IsCalibrating = false;
                MessageInfo = "Калибровка сохранена.";
                StatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                MessageInfo = $"Ошибка сохранения калибровки: {ex.Message}";
                StatusColor = Colors.Red;
            }
        }

        private void LoadCalibration()
        {
            try
            {
                var json = Preferences.Get(CalibrationPrefsKey, string.Empty);
                if (string.IsNullOrEmpty(json)) return;

                var d = JsonSerializer.Deserialize<CalibrationData>(json);
                if (d == null) return;

                ThumbFlex = d.ThumbFlex; ThumbExtend = d.ThumbExtend;
                IndexFlex = d.IndexFlex; IndexExtend = d.IndexExtend;
                MiddleFlex = d.MiddleFlex; MiddleExtend = d.MiddleExtend;
                RingFlex = d.RingFlex; RingExtend = d.RingExtend;
                PinkyFlex = d.PinkyFlex; PinkyExtend = d.PinkyExtend;
            }
            catch { /* игнорируем, откроется с дефолтами */ }
        }
        #endregion
    }

    public class CalibrationData
    {
        public double ThumbFlex { get; set; }
        public double ThumbExtend { get; set; }
        public double IndexFlex { get; set; }
        public double IndexExtend { get; set; }
        public double MiddleFlex { get; set; }
        public double MiddleExtend { get; set; }
        public double RingFlex { get; set; }
        public double RingExtend { get; set; }
        public double PinkyFlex { get; set; }
        public double PinkyExtend { get; set; }
    }
}