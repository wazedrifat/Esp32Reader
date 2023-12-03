using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esp32Reader
{
    [Serializable]
    public class ESP32Data
    {
        public float Steering;
        public int Gear;
        public float Accerlator;
        public float Clutch;
        public float Break;
        public CarStatus Status;

        public bool HasStatus(CarStatus status) => (this.Status & status) == status;

        public CarStatus HasMultipleStatus(CarStatus status) => this.Status & status;

        public void SetStatus(CarStatus status)
        {
            this.Status |= status;
        }

        public void RemoveStatus(CarStatus status)
        {
            this.Status &= ~status;
        }

        public void ToogleStatus(CarStatus status)
        {
            this.Status ^= status;
        }

        public int GetGear()
        {
            return this.Gear switch
            {
                6 => -1,
                _ => this.Gear - 1
            };
        }
    }

    [Flags]
    public enum CarStatus
    {
        None = 0,
        HeadLight = 1,
        Horn = 2,
        Engine = 4,
        Wiper = 8,
        LeftIndicator = 16,
        RightIndicator = 32,
        Seatbelt = 64,
        HandBrake = 128,
    }
}
