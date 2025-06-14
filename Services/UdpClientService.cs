using MirroRehab.Interfaces;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MirroRehab.Services
{
  
    public class UdpClientService : IUdpClientService
    {
        private readonly UdpClient _client;
        private  IPEndPoint _remoteEP;

        public UdpClientService()
        {
            _client = new UdpClient();
            _client.Client.ReceiveTimeout = 5000; // Таймаут 5 секунд
            int port = DeviceInfo.Platform == DevicePlatform.Android ? 43450 : 53452;
            _remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);  //на пк - 53452
        }

        public async Task PingSensoAsync()
        {
            try
            {
                string messageString = "{\"type\":\"ping\"}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageString);
                await _client.SendAsync(messageBytes, messageBytes.Length, _remoteEP);
                Debug.WriteLine($"[UdpClientService] Отправлен пакет: {messageString} на {_remoteEP}");
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке ping: {ex.Message}");
                throw;
            }
        }

        public async Task<JsonModel> ReceiveDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Отправка "ping"
                await PingSensoAsync();

                
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
                {
                    Debug.WriteLine("[UdpClientService] Ожидание ответа от сервера...");
                    UdpReceiveResult receiveResult = await _client.ReceiveAsync(linkedCts.Token);
                    Debug.WriteLine("[UdpClientService] Ответ получен");
                    string receiveString = Encoding.UTF8.GetString(receiveResult.Buffer).TrimEnd('\0');
                    Debug.WriteLine($"[UdpClientService] данные получены");

                    // Десериализация JSON
                    JsonModel data = JsonConvert.DeserializeObject<JsonModel>(receiveString);
                    Debug.WriteLine($"[UdpClientService] Десериализовано: type={data?.type}, src={data?.src}");

                    return data;
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка сокета: {ex.Message}, Код ошибки: {ex.SocketErrorCode}");
                return null;
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine($"[UdpClientService] Операция отменена: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка десериализации JSON: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка получения данных: {ex.Message}, StackTrace: {ex.StackTrace}");
                return null;
            }
        }
        // Для обратной совместимости
        public JsonModel ReceiveData()
        {
            try
            {
                // Отправка "ping"
                string messageString = "ping";
                byte[] messageBytes = Encoding.ASCII.GetBytes(messageString);
                _client.Send(messageBytes, messageBytes.Length, _remoteEP);
                Debug.WriteLine($"[UdpClientService] Отправлен ping на {_remoteEP}");

                // Получение ответа
                byte[] receiveBytes = _client.Receive(ref _remoteEP);
                string receiveString = Encoding.ASCII.GetString(receiveBytes).TrimEnd('\0');
                Debug.WriteLine($"[UdpClientService] Получены данные: {receiveString}");

                // Десериализация JSON
                JsonModel data = JsonConvert.DeserializeObject<JsonModel>(receiveString);
                Debug.WriteLine($"[UdpClientService] Десериализовано: {data}");

                return data;
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка сокета: {ex.Message}, Код ошибки: {ex.SocketErrorCode}");
                return null;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка десериализации JSON: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UdpClientService] Ошибка получения данных: {ex.Message}");
                return null;
            }
        }
    }
}