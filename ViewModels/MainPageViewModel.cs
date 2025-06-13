using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MirroRehab.Interfaces;
using MirroRehab.Models;
using MirroRehab.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace MirroRehab.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly ICalibrationService _calibrationService;
        private readonly IUdpClientService _udpClientService;
        private readonly IPositionProcessor _positionProcessor;
        private readonly HandController _handController;
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

        public MainPageViewModel(
            IBluetoothService bluetoothService,
            ICalibrationService calibrationService,
            IUdpClientService udpClientService,
            IPositionProcessor positionProcessor,
            HandController handController)
        {
            _bluetoothService = bluetoothService;
            _calibrationService = calibrationService;
            _udpClientService = udpClientService;
            _positionProcessor = positionProcessor;
            _handController = handController;
            IsBusy = true;
            LoadSavedDevices();
            Task.Run(InitialSearchDevicesAsync);
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
                if (IsBusy) return;
                IsBusy = true;
                ShowError = false;
                MessageInfo = "Поиск устройств MirroRehab...";
                StatusColor = Colors.Black;

                var foundDevices = await _bluetoothService.DiscoverMirroRehabDevicesAsync();
                Devices.Clear();
                foreach (var device in foundDevices)
                {
                    if (!Devices.Any(d => d.Address == device.Address))
                    {
                        Devices.Add(device);
                    }
                }

                SaveDevices();
                MessageInfo = Devices.Any() ? $"Найдено устройств: {Devices.Count}" : "Устройства MirroRehab не найдены";
                StatusColor = Devices.Any() ? Colors.Green : Colors.Yellow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска: {ex.Message}");
                MessageInfo = $"Ошибка: {ex.Message}";
                ShowError = true;
                StatusColor = Colors.Yellow;
            }
            finally
            {
                IsBusy = false;
                IsCalibrated = true;
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

            if (_connectedDevice == null || !_bluetoothService.IsConnected || _connectedDevice.Address != SelectedDevice.Address)
            {
                Debug.WriteLine($"Подключение к {SelectedDevice.Name}...");
                _connectedDevice = await _bluetoothService.ConnectToDeviceAsync(SelectedDevice.Address);
                IsConnected = _bluetoothService.IsConnected;
                MessageInfo = $"Подключено к {SelectedDevice.Name}";
                StatusColor = Colors.Green;
            }
        }

        [RelayCommand]
        private async Task CalibrateDevice()
        {
            try
            {
                if (IsBusy) return;
                IsBusy = true;
                await EnsureConnected();
                await _calibrationService.CalibrateMinAsync(_connectedDevice);
                await _calibrationService.CalibrateMaxAsync(_connectedDevice);
                IsCalibrated = true;
                MessageInfo = "Калибровка завершена!";
                StatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка калибровки: {ex.Message}");
                MessageInfo = $"Ошибка калибровки: {ex.Message}";
                StatusColor = Colors.Yellow;
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
                if (IsBusy || !IsCalibrated) return;
                IsBusy = true;
                await EnsureConnected();
                _cancellationTokenSource = new CancellationTokenSource();
                IsRunning = true;
                Debug.WriteLine($"Запуск отслеживания...");

                await _handController.StartTracking(_cancellationTokenSource.Token, SelectedDevice);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отслеживания: {ex.Message}");
                MessageInfo = $"Ошибка отслеживания: {ex.Message}";
                StatusColor = Colors.Yellow;
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
            _bluetoothService.DisconnectDevice();
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
                    StatusColor = Colors.Yellow;
                    return;
                }
                IsBusy = true;
                await EnsureConnected();
                _cancellationTokenSource = new CancellationTokenSource();
                IsRunning = true;
                MessageInfo = "Запуск демо...";
                StatusColor = Colors.Black;

                await _handController.DemoMirro(_cancellationTokenSource.Token, SelectedDevice);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в демо: {ex.Message}");
                MessageInfo = $"Ошибка демо: {ex.Message}";
                StatusColor = Colors.Yellow;
            }
            finally
            {
                IsBusy = false;
                IsRunning = false;
            }
        }
    }
}