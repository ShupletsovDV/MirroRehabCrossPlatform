
using MirroRehab.Services;
using System.Threading.Tasks;

namespace MirroRehab.Interfaces
{
    public interface IPositionProcessor
    {
        Task<byte[]> ProcessPositionAsync(JsonModel data, IBluetoothService device);
    }
}