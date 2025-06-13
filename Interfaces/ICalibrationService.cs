
namespace MirroRehab.Interfaces
{
    public interface ICalibrationService
    {
        Task CalibrateMinAsync(IDevice device);
        Task CalibrateMaxAsync(IDevice device);
    }
}