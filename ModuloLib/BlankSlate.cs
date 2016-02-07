namespace ModuloLib
{
    public class BlankSlate : Modulo
    {
        const byte kFUNCTION_GET_DIGITAL_INPUT = 0;
        const byte kFUNCTION_GET_DIGITAL_INPUTS = 1;
        const byte kFUNCTION_GET_ANALOG_INPUT = 2;
        const byte kFUNCTION_SET_DATA_DIRECTION = 3;
        const byte kFUNCTION_SET_DATA_DIRECTIONS = 4;
        const byte kFUNCTION_SET_DIGITAL_OUTPUT = 5;
        const byte kFUNCTION_SET_DIGITAL_OUTPUTS = 6;
        const byte kFUNCTION_SET_PWM_OUTPUT = 7;
        const byte kFUNCTION_SET_PULLUP = 8;
        const byte kFUNCTION_SET_PULLUPS = 9;
        const byte kFUNCTION_SET_PWM_FREQUENCY = 10;

        public bool Initialize(Port port, ushort? deviceId)
        {
            return base.Initialize(port, "co.modulo.blankslate", deviceId);
        }
        // """Disables the output on the specified pin and returns the pin's value"""
        public byte? GetDigitalInput(byte pin)
        {
            byte[] result = transfer(kFUNCTION_GET_DIGITAL_INPUT, new byte[] { pin }, 1);
            if(result != null)
            {
                return result[0];
            }
            return null;
        }
        //"""Reads the digital inputs from all 8 pins. Does not enable/disable outputs on any pins."""
        public byte? GetDigitalInputs()
        {
            byte[] result = transfer(kFUNCTION_GET_DIGITAL_INPUTS, null, 1);
            if (result != null)
            {
                return result[0];
            }
            return null;
        }
        //"""Disables the output on the specified pin and performs an analog read."""
        public ushort? GetAnalogInput(byte pin)
        {
            byte[] result = transfer(kFUNCTION_GET_ANALOG_INPUT, new byte[] { pin }, 1);
            if (result != null)
            {
                return (ushort)((result[0] | (result[1] << 8))/1023.0);
            }
            return null;
        }
        //  """Sets the pin direction to either output or input"""
        public void SetDirection(byte pin, byte output)
        {
            transfer(kFUNCTION_SET_DATA_DIRECTION, new byte[] { pin, output }, 0);
        }
        //"""Sets the pin directions for all 8 pins simultaneously"""
        public void SetDirections(byte outputs)
        {
            transfer(kFUNCTION_SET_DATA_DIRECTIONS, new byte[] { outputs }, 0);
        }
        //"""Enables the output and sets the output value on the specified pin."""
        public void SetDigitalOutput(byte pin,byte value)
        {
            transfer(kFUNCTION_SET_DIGITAL_OUTPUTS, new byte[] {pin, value }, 0);
        }
        //"""Set the digital outputs on all 8 pins. Does not enable or disable outputs on any pins."""
        public void SetDigitalOutputs(byte values)
        {
            transfer(kFUNCTION_SET_DIGITAL_OUTPUTS, new byte[] { values }, 0);
        }
        // """Enable the output and set the PWM duty cycle on the specified pin.
        //  Pins 0-4 have hardware PWM support.Pins 5-7 only have software PWM
        //  which has more jitter, especially at high frequencies."""
        public void setPWMValue(byte pin, byte value)
        {
            if (value >= 1)
            {
                SetDigitalOutput(pin, 1);
            }
            if (value <= 0)
            {
                SetDigitalOutput(pin, 0);
            }

            int v = (int)(65535 * value);
            byte[] sendData = new byte[] { pin, (byte)(v & 0xFF), (byte)(v >> 8) };

            transfer(kFUNCTION_SET_PWM_OUTPUT, sendData, 0);
        }
        //"""Sets whether a pullup is enabled on the specified pin."""
        public void SetPullup(byte pin, byte enable)
        {
            transfer(kFUNCTION_SET_PULLUP, new byte[] { pin, enable }, 0);
        }
        //"""Set whether the pullup is enabled on all 8 pins."""
        public void SetPullups(byte values)
        {
            transfer(kFUNCTION_SET_PULLUPS, new byte[] { values }, 0);
        }
        //"""Set the frequency for PWM signals on the specified pin."""
        public void SetPWMFrequency(byte pin, ushort value)
        {
            byte[] sendData = new byte[] { pin, (byte)(value & 0xFF), (byte)(value >> 8) };

            transfer(kFUNCTION_SET_PWM_FREQUENCY, sendData, 0);
        }
    }
}
