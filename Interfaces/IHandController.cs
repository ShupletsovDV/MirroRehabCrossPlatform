using System.Threading;
using System.Threading.Tasks;
using MirroRehab.Interfaces;

namespace MirroRehab.Interfaces
{
    public interface IHandController
    {
        Task<bool> CalibrateDevice(IDevice device);
        Task<bool> StartTracking(CancellationToken cancellationToken, IDevice device);
        Task DemoMirro(CancellationToken cancellationToken, IDevice device);
    }
}