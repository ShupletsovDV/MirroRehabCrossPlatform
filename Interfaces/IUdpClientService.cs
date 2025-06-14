using System.Threading.Tasks;

namespace MirroRehab.Interfaces
{
    public interface IUdpClientService
    {
        Task PingSensoAsync();
        JsonModel ReceiveData();
        Task<JsonModel> ReceiveDataAsync(CancellationToken cancellationToken);
    }
}