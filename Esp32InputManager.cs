using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Esp32Reader
{
    public class Esp32InputManager
    {
        private Esp32Setting Esp32Setting;
        private SerialPort SerialPortForSteering;
        private SerialPort SerialPortForOther;
        public ESP32Data Data = new ESP32Data(); // Reference to your ESP32Data object in the Inspector

        public string Esp32StringSteeringData = string.Empty;
        public string Esp32StringOtherData = string.Empty;

        #region PreCalculate
        private float SteeringRangeDiff { get; set; }
        private float SteeringPartialValue { get; set; }
        private float AccerlatorRangeDiff { get; set; }
        private float AccerlatorPartialValue { get; set; }
        private float ClutchRangeDiff { get; set; }
        private float ClutchPartialValue { get; set; }
        private float BreakRangeDiff { get; set; }
        private float BreakPartialValue { get; set; }

        private void CalculateInitialValues(Esp32Setting esp32Setting)
        {
            this.SteeringRangeDiff = this.GetRange(esp32Setting.Steering.Expectation) / this.GetRange(esp32Setting.Steering.Input);
            this.SteeringPartialValue = esp32Setting.Steering.Expectation.MinValue - esp32Setting.Steering.Input.MinValue * SteeringRangeDiff;

            this.AccerlatorRangeDiff = this.GetRange(esp32Setting.Accerlator.Expectation) / this.GetRange(esp32Setting.Accerlator.Input);
            this.AccerlatorPartialValue = esp32Setting.Accerlator.Expectation.MinValue - esp32Setting.Accerlator.Input.MinValue * AccerlatorRangeDiff;

            this.ClutchRangeDiff = this.GetRange(esp32Setting.Clutch.Expectation) / this.GetRange(esp32Setting.Clutch.Input);
            this.ClutchPartialValue = esp32Setting.Clutch.Expectation.MinValue - esp32Setting.Clutch.Input.MinValue * ClutchRangeDiff;

            this.BreakRangeDiff = this.GetRange(esp32Setting.Break.Expectation) / this.GetRange(esp32Setting.Break.Input);
            this.BreakPartialValue = esp32Setting.Break.Expectation.MinValue - esp32Setting.Break.Input.MinValue * BreakRangeDiff;
        }

        private float GetRange(ValueRange range)
        {
            return range.MaxValue - range.MinValue;
        }
        #endregion

        public Esp32InputManager()
        {
            string CustomGameSettingPath = @$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}Low\CodeSimBD\Driving-Simulator\GameSetting.json";
            this.Esp32Setting = JsonConvert.DeserializeObject<GameSetting>(File.ReadAllText(CustomGameSettingPath))?.Esp32;
            //this.SerialPortForSteering = new SerialPort(this.Esp32Setting.PortNameForSteering, this.Esp32Setting.BaudRate);
            this.SerialPortForOther = new SerialPort(this.Esp32Setting.PortNameForOther, this.Esp32Setting.BaudRate);
            OpenSerialPort();
            RecieveESPDataLoop();
        }

        void OnDestroy()
        {
            //this.SerialPortForSteering.Close();
            this.SerialPortForOther.Close();
        }

        void OpenSerialPort()
        {
            bool err = false;
            //if (!this.SerialPortForSteering.IsOpen)
            //{
            //    try
            //    {
            //        this.SerialPortForSteering.Open();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Failed to open steering serial port cuz {ex.Message}");
            //        err = true;
            //    }

            //}

            if (!this.SerialPortForOther.IsOpen)
            {
                try
                {
                    this.SerialPortForOther.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open other serial port cuz {ex.Message}");
                    err = true;
                }
            }
        }

        void RecieveESPDataLoop()
        {
            Task.Run(() =>
            {
                var sw = new Stopwatch();
                while (true)
                {
                    sw.Restart();
                    //if (this.SerialPortForSteering.IsOpen)
                    //    this.Esp32StringSteeringData = this.SerialPortForSteering.ReadLine();
                    if (this.SerialPortForOther.IsOpen)
                    {
                        this.Esp32StringOtherData = this.SerialPortForOther.ReadLine();
                        this.Esp32StringOtherData = this.SerialPortForOther.ReadLine();
                    }
                    Console.Write(sw.ElapsedMilliseconds + "\t");
                    RecieveESPData();
                    sw.Stop();
                    Console.WriteLine(sw.ElapsedMilliseconds);
                }
            });
        }

        void RecieveESPData()
        {
            var steeringData = string.IsNullOrEmpty(this.Esp32StringSteeringData) ? "0" : this.Esp32StringSteeringData;
            var integers = this.Esp32StringOtherData.Split(',');

            if (integers.Length != 5)
            {
                integers = new string[] { "1", "0.0", "0.0", "1", "0" };
            }

            this.Data = new ESP32Data()
            {
                Steering = -1 * this.Normalize(steeringData, this.SteeringRangeDiff, this.SteeringPartialValue, this.Esp32Setting.Steering),
                Gear = int.Parse(integers[0]),
                Accerlator = this.Normalize(integers[1], this.AccerlatorRangeDiff, this.AccerlatorPartialValue, this.Esp32Setting.Accerlator),
                Clutch = this.Normalize(integers[2], this.ClutchRangeDiff, this.ClutchPartialValue, this.Esp32Setting.Clutch),
                Break = this.Normalize(integers[3], this.BreakRangeDiff, this.BreakPartialValue, this.Esp32Setting.Break),
                Status = (CarStatus)int.Parse(integers[4]),
            };

            //var statuses = new StringBuilder();
            //if (Data.HasStatus(CarStatus.HeadLight)) statuses.Append("HeadLight-");
            //if (Data.HasStatus(CarStatus.Horn)) statuses.Append("Horn-");
            //if (Data.HasStatus(CarStatus.Engine)) statuses.Append("engine-");
            //if (Data.HasStatus(CarStatus.Wiper)) statuses.Append("wiper-");
            //if (Data.HasStatus(CarStatus.LeftIndicator)) statuses.Append("LeftIdicatior-");
            //if (Data.HasStatus(CarStatus.RightIndicator)) statuses.Append("RightIndicator-");

            //Console.WriteLine($"{Data.Steering} & {Data.Gear},{Data.Accerlator},{Data.Clutch},{Data.Break},{Convert.ToString((int)Data.Status, 2)} -> {statuses}");
        }

        private float Normalize(string value, float rangeDiff, float partialValue, PropertyValue property)
        {
            if (!float.TryParse(value, out var floatValue))
            {
                return 0f;
            }

            if (rangeDiff < 0.000000001)
            {
                return Math.Clamp(floatValue, property.Expectation.MinValue, property.Expectation.MaxValue);
            }
            return Math.Clamp(partialValue + floatValue * rangeDiff, property.Expectation.MinValue, property.Expectation.MaxValue);
        }
    }

}
