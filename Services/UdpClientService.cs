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
        private readonly IPEndPoint _remoteEP;

        public UdpClientService()
        {
            _client = new UdpClient();
            _client.Client.ReceiveTimeout = 5000; // Таймаут 5 секунд
            _remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53452);
        }

        public async Task StartPingAsync()
        {
            try
            {
                var pingBytes = Encoding.ASCII.GetBytes("ping");
                await _client.SendAsync(pingBytes, pingBytes.Length, _remoteEP);
                System.Diagnostics.Debug.WriteLine("Ping отправлен на 127.0.0.1:53452");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке ping: {ex.Message}");
                throw;
            }
        }

        public async Task<JsonModel> ReceiveDataAsync()
        {
            try
            {
                var messageBytes = Encoding.ASCII.GetBytes("ping");
                await _client.SendAsync(messageBytes, messageBytes.Length, _remoteEP);
                System.Diagnostics.Debug.WriteLine("Ping отправлен на 127.0.0.1:53452");

                var receiveResult = await _client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
                var data = Encoding.ASCII.GetString(receiveResult.Buffer).TrimEnd('\0');

                // Исправляем потенциально некорректный JSON
                if (data.EndsWith("]"))
                {
                    data += "}";
                }
                if (!data.EndsWith("}") && !string.IsNullOrEmpty(data))
                {
                    data = data.Substring(0, data.Length - 1);
                }

                System.Diagnostics.Debug.WriteLine($"Получены данные: {data}");

                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                var jsonModel = JsonConvert.DeserializeObject<JsonModel>(data);
                return jsonModel;
            }
            catch (TimeoutException)
            {
                System.Diagnostics.Debug.WriteLine("Таймаут получения данных от сервера");
                return null;
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сокета: {ex.Message}, Код ошибки: {ex.SocketErrorCode}");
                return null;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка десериализации JSON: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения данных: {ex.Message}");
                return null;
            }
        }
    }
}