using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esp32Reader
{
    [Serializable]
    public class GameSetting
    {
        public Esp32Setting Esp32 { get; set; }
    }

    [Serializable]
    public class Esp32Setting
    {
        public string PortNameForSteering { get; set; }
        public string PortNameForOther { get; set; }
        public int BaudRate { get; set; }
        public float RecieveDatainterval { get; set; }

        public PropertyValue Steering { get; set; }
        public PropertyValue Accerlator { get; set; }
        public PropertyValue Clutch { get; set; }
        public PropertyValue Break { get; set; }
    }

    public class PropertyValue
    {
        public ValueRange Expectation { get; set; }
        public ValueRange Input { get; set; }
    }

    public class ValueRange
    {
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
    }
}
