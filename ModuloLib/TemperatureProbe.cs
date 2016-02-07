using System;

namespace ModuloLib
{
    public class TemperatureProbe : Modulo
    {
        const byte kFunctionGetTemperature = 0;
        const byte kEventTemperatureChanged = 0;

        protected bool isValid = false;
        private float temperature;      
        Action<TemperatureProbe> temperatureChangedCallback = null;

        public bool Initialize(Port port,
                               ushort? deviceId = null,
                               Action<TemperatureProbe> tempCallback = null)
        {
            if (base.Initialize(port, "co.modulo.tempprobe", deviceId))
            {
                temperatureChangedCallback = tempCallback;
                return true;
            }
            return false;
        }
        public float GetTemperatureCelsius()
        {
            return temperature / 10.0f;
        }
        public float GetTemperatureFahrenheit()
        {
            return temperature * 1.8f / 10.0f + 32;
        }
        protected override bool init()
        {
            if(base.init())
            {
                byte[] received = transfer(kFunctionGetTemperature, null, 2);
                if(received == null)
                {
                    isValid = false;
                    return false;
                }
                isValid = true;
                temperature = (float)(received[0] | received[1] << 8);
                if(temperatureChangedCallback != null)
                {
                    temperatureChangedCallback(this);
                }
            }
            return false;
        }
        public override void ProcessEvent(byte code, ushort data)
        {
            if(code == kEventTemperatureChanged)
            {
                temperature = data;
                isValid = true;
                if(temperatureChangedCallback != null)
                {
                    temperatureChangedCallback(this);
                }
            }
        }
    }
}
