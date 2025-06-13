using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirroRehab
{
    public class Data
    {
        public string type { get; set; }
        public Palm palm { get; set; }
        public List<double> gests { get; set; }
        public Wrist wrist { get; set; }
        public List<Finger> fingers { get; set; }
        public List<int> m1 { get; set; }
        public int h_rssi { get; set; }
        public int h_gain { get; set; }
        public int g_rssi { get; set; }
        public int g_gain { get; set; }
    }

    public class Finger
    {
        public List<double> ang { get; set; }
        public List<double> quat { get; set; }
        public List<double> q { get; set; }
        public double bend { get; set; }
        public List<double> quat2 { get; set; }
        public double spd { get; set; }
        public List<double> grv { get; set; }
        public List<double> lia { get; set; }
        public List<double> grv2 { get; set; }
        public List<double> lia2 { get; set; }
    }

    public class Palm
    {
        public List<double> pos { get; set; }
        public List<double> spd { get; set; }
        public List<double> quat { get; set; }
        public List<double> grv { get; set; }
        public List<double> lia { get; set; }
        public double delta { get; set; }
    }

    public class JsonModel
    {
        public string src { get; set; }
        public string version { get; set; }
        public string name { get; set; }
        public string fullname { get; set; }
        public string type { get; set; }
        public Data data { get; set; }
    }

    public class Wrist
    {
        public List<double> quat { get; set; }
        public List<double> grv { get; set; }
        public List<double> lia { get; set; }
        public double delta { get; set; }
    }
}
