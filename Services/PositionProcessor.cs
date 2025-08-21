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
            if (angIndex < 0)
            {
                angIndex = 0.0;
            }
            if (angIndex > 3)
            {
                angIndex = 3;
            }

            if (angMiddle < 0)
            {
                angMiddle = 0.0;
            }
            if (angMiddle > 3)
            {
                angMiddle = 3;
            }

            if (angPinky < 0)
            {
                angPinky = 0.0;
            }
            if (angPinky > 3)
            {
                angPinky = 3;
            }

            if (angRing < 0)
            {
                angRing = 0.0;
            }
            if (angRing > 3)
            {
                angRing = 3;
            }
            return (angIndex, angMiddle, angRing, angPinky);
        }

    }
}