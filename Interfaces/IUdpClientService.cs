using System.Threading.Tasks;

namespace MirroRehab.Interfaces
{
    public interface IUdpClientService
    {
        Task StartPingAsync();
        Task<JsonModel> ReceiveDataAsync();
    }
}