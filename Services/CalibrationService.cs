using MirroRehab.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirroRehab.Services
{
    public class CalibrationService: ICalibrationService
    {
        public CalibrationService() { }
       public Task CalibrateMinAsync(IDevice device)
       {
            return Task.FromResult(0);
       }
       public Task CalibrateMaxAsync(IDevice device)
       {
            return Task.FromResult(0);
       }
    }
}
