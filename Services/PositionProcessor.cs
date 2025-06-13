using MirroRehab.Interfaces;
using MirroRehab.Services;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MirroRehab.Services
{
    public class PositionProcessor : IPositionProcessor
    {
        private readonly Dictionaries _dictionaries;

        public PositionProcessor(Dictionaries dictionaries)
        {
            _dictionaries = dictionaries;
        }

        public async Task<byte[]> ProcessPositionAsync(JsonModel data, IBluetoothService device)
        {
            try
            {
                dynamic jsonData = data;
                double angIndex = Math.Round((double)jsonData.data.fingers[1].ang[0], 1);
                double angMiddle = Math.Round((double)jsonData.data.fingers[2].ang[0], 1);
                double angRing = Math.Round((double)jsonData.data.fingers[3].ang[0], 1);
                double angPinky = Math.Round((double)jsonData.data.fingers[4].ang[0], 1);

                (angIndex, angMiddle, angRing, angPinky) = NormalizeAngles(angIndex, angMiddle, angRing, angPinky);

                string dataString = jsonData.data.type == "lh"
                    ? $"{_dictionaries.DictIndex[angIndex]},{_dictionaries.DictMiddle[angMiddle]},{_dictionaries.DictRing[angRing]},{_dictionaries.DictPinky[angPinky]},0"
                    : $"{_dictionaries.DictPinkyRight[angPinky]},{_dictionaries.DictRingRight[angRing]},{_dictionaries.DictMiddleRight[angMiddle]},{_dictionaries.DictIndexRight[angIndex]},0";

                await device.SendDataAsync(dataString);
                Debug.WriteLine($"данные отправлены:{dataString}");
                /*  var service = await device.GetServiceAsync(Guid.Parse("your_data_service_uuid"));
                  var characteristic = await service.GetCharacteristicAsync(Guid.Parse("your_data_characteristic_uuid"));
                  await characteristic.WriteAsync(System.Text.Encoding.ASCII.GetBytes(dataString + "\n"));*/
                return System.Text.Encoding.ASCII.GetBytes(dataString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка обработки данных: " + ex.Message);
            }
        }

        private (double, double, double, double) NormalizeAngles(double angIndex, double angMiddle, double angRing, double angPinky)
        {
            angIndex = Math.Max(0, Math.Min(angIndex, _dictionaries.MaxIndex));
            angMiddle = Math.Max(0, Math.Min(angMiddle, _dictionaries.MaxMiddle));
            angRing = Math.Max(0, Math.Min(angRing, _dictionaries.MaxRing));
            angPinky = Math.Max(0, Math.Min(angPinky, _dictionaries.MaxPinky));
            return (angIndex, angMiddle, angRing, angPinky);
        }
    }
}