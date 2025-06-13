using System.Collections.Generic;

namespace MirroRehab.Services
{
    public class Dictionaries
    {
        public int MaxIndex { get; set; } = 170;
        public int MaxMiddle { get; set; } = 0;
        public int MaxRing { get; set; } = 170;
        public int MaxPinky { get; set; } = 0;

        public int MinIndex { get; set; } = 0;
        public int MinMiddle { get; set; } = 170;
        public int MinRing { get; set; } = 0;
        public int MinPinky { get; set; } = 170;

        public int MaxIndexRight { get; set; } = 0;
        public int MaxMiddleRight { get; set; } = 170;
        public int MaxRingRight { get; set; } = 0;
        public int MaxPinkyRight { get; set; } = 170;

        public int MinIndexRight { get; set; } = 170;
        public int MinMiddleRight { get; set; } = 0;
        public int MinRingRight { get; set; } = 170;
        public int MinPinkyRight { get; set; } = 0;

        private readonly Dictionary<double, int> _myDict = new()
        {
            {0.0, 0}, {0.1, 2}, {0.2, 4}, {0.3, 8}, {0.4, 12}, {0.5, 14}, {0.6, 16},
            {0.7, 18}, {0.8, 20}, {0.9, 22}, {1.0, 24}, {1.1, 26}, {1.2, 28}, {1.3, 30},
            {1.4, 32}, {1.5, 34}, {1.6, 36}, {1.7, 38}, {1.8, 40}, {1.9, 42}, {2.0, 44},
            {2.1, 46}, {2.2, 48}, {2.3, 50}, {2.4, 52}, {2.5, 54}, {2.6, 56}, {2.7, 58},
            {2.8, 60}, {2.9, 62}, {3.0, 68}
        };

        private readonly Dictionary<double, int> _myDictReverse = new()
        {
            {0.0, 68}, {0.1, 64}, {0.2, 62}, {0.3, 60}, {0.4, 58}, {0.5, 56}, {0.6, 54},
            {0.7, 52}, {0.8, 50}, {0.9, 48}, {1.0, 46}, {1.1, 44}, {1.2, 42}, {1.3, 40},
            {1.4, 38}, {1.5, 36}, {1.6, 34}, {1.7, 32}, {1.8, 30}, {1.9, 28}, {2.0, 26},
            {2.1, 24}, {2.2, 22}, {2.3, 20}, {2.4, 18}, {2.5, 16}, {2.6, 14}, {2.7, 12},
            {2.8, 10}, {2.9, 8}, {3.0, 2}
        };

        public Dictionary<double, int> DictIndex { get; private set; }
        public Dictionary<double, int> DictMiddle { get; private set; }
        public Dictionary<double, int> DictRing { get; private set; }
        public Dictionary<double, int> DictPinky { get; private set; }
        public Dictionary<double, int> DictIndexRight { get; private set; }
        public Dictionary<double, int> DictMiddleRight { get; private set; }
        public Dictionary<double, int> DictRingRight { get; private set; }
        public Dictionary<double, int> DictPinkyRight { get; private set; }

        public Dictionaries()
        {
            UpdateDictionaries();
        }

        public void UpdateDictionaries()
        {
            DictIndex = RedistributeValues(_myDict, MinIndex, MaxIndex);
            DictMiddle = RedistributeValues(_myDictReverse, MinMiddle, MaxMiddle, true);
            DictRing = RedistributeValues(_myDict, MinRing, MaxRing);
            DictPinky = RedistributeValues(_myDictReverse, MinPinky, MaxPinky, true);
            DictIndexRight = RedistributeValues(_myDictReverse, MinIndexRight, MaxIndexRight, true);
            DictMiddleRight = RedistributeValues(_myDict, MinMiddleRight, MaxMiddleRight);
            DictRingRight = RedistributeValues(_myDictReverse, MinRingRight, MaxRingRight, true);
            DictPinkyRight = RedistributeValues(_myDict, MinPinkyRight, MaxPinkyRight);
        }

        private Dictionary<double, int> RedistributeValues(Dictionary<double, int> dict, int minValue, int maxValue, bool reverse = false)
        {
            if (dict.Count == 0) return new Dictionary<double, int>();
            int oldMin = dict.Values.Min();
            int oldMax = dict.Values.Max();

            if (oldMin == oldMax)
                return dict.ToDictionary(kvp => kvp.Key, kvp => minValue);

            return dict.ToDictionary(kvp =>
            {
                int oldValue = kvp.Value;
                if (reverse) oldValue = oldMax - (oldValue - oldMin);
                return kvp.Key;
            }, kvp =>
            {
                int oldValue = kvp.Value;
                if (reverse) oldValue = oldMax - (oldValue - oldMin);
                return (int)Math.Round(((double)(oldValue - oldMin) / (oldMax - oldMin)) * (maxValue - minValue) + minValue);
            });
        }
    }
}