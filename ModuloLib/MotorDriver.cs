using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    public class MotorDriver : Modulo
    {
        const byte kModeDisabled = 0;
        const byte kModeDC = 1;
        const byte kModeStepper = 2;

        const byte kFunctionSetValue = 0;
        const byte kFunctionSetEnabled = 1;
        const byte kFunctionSetFrequency = 2;
        const byte kFunctionSetCurrentLimit = 3;
        const byte kFunctionSetStepperSpeed = 4;
        const byte kFunctionGetStepperPosition = 5;
        const byte kFunctionSetStepperTarget = 6;
        const byte kFunctionAddStepperOffset = 7;

        const byte kEventPositionReached = 0;
        const byte kEventFaultChanged = 1;

        Action<MotorDriver> positionReachedCallback = null;
        Action<MotorDriver> faultChangedCallback = null;

        bool fault;
        int usPerStep = 5000;
        int microSteps = 256;
        int minMicrostepDuration = 1000;

        public bool Initialize(Port port,
                               ushort? deviceId = null,
                               Action<MotorDriver> psitionReachedCallback = null,
                               Action<MotorDriver> fultChangedCallback = null)
        {
            positionReachedCallback = psitionReachedCallback;
            faultChangedCallback = fultChangedCallback;
            return base.Initialize(port, "co.modulo.motor", deviceId);
        }
        //"""Set a single channel (0-3) to the specified amount, between 0 and 1.
        // Changes the mode to ModeDC if it's not already."""
        public void SetChannel(byte channel, float amount)
        {
            int intValue = (int)amount.Clamp(0, 1) * 0xFFFF;
            byte[] data = new byte[] { channel, (byte)(intValue & 0xFF), (byte)(intValue >> 8) };
            transfer(kFunctionSetValue, data, 0);
        }
        //"""Sets the motor output for a side (A=0,B=2) to a specified value.
        //Includes a -1<=x<=1 check on value to prevent silent failure."""
        public void SetMotor(byte side, int value)
        {
            value = value.Clamp(-1, 1);
            if (value > 0)
            {
                SetChannel(side, 1);
                SetChannel((byte)(side + 1), 1 - value);
            }
            else
            {
                SetChannel(side, 1 + value);
                SetChannel((byte)(side + 1), 1);
            }
        }
        //   """Set the motor output A to the specified amount, between -1 and 1.
        // Changes the mode to ModeDC if it's not already."""
        public void SetMotorA(int value)
        {
            SetMotor(0, value);
        }
        //"""Set the motor output B to the specified amount, between -1 and 1.
        // Changes the mode to ModeDC if it's not already."""
        public void SetMotorB(int value)
        {
            SetMotor(2, value);
        }
        //"""Set the driver mode to Disabled, DC, or Stepper"""
        public void SetMode(byte mode)
        {
            transfer(kFunctionSetEnabled, new byte[] { mode }, 0);
        }
        //        """Set the driver current limit (between 0 and 1)."""
        public void SetCurrentLimit(float limit)
        {
            byte[] data = new byte[] { (byte)(limit.Clamp(0, 1) * 63) };
            transfer(kFunctionSetCurrentLimit, data, 0);
        }
        //"""Set the motor driver PWM frequency"""
        public void SetPWMFrequency(ushort freq)
        {
            byte[] data = new byte[] { (byte)(freq & 0xFF), (byte)(freq >> 8) };
            transfer(kFunctionSetFrequency, data, 0);
        }
        //"""Set the stepper speed in whole steps per second."""
        public void SetStepperSpeed(float stepsPerSecond)
        {
            SetStepperRate((int)(1.0e6 / stepsPerSecond));
        }
        //"""Set the number of microseconds to take between each whole step."""
        public void SetStepperRate(int usPerStep)
        {
            this.usPerStep = usPerStep;
            UpdateStepperSpeed();
        }
        /*"""Set the number of microsteps to take between each whole step.
            It can be 1, 2, 4, 8, 16, 32, 64, 128, or 256.
            If the duration of a microstep(in microseconds) would be less than
            minMicrostepDuration, then the number of microsteps is decreased
            automatically.This helps to avoid skipping steps when the rate is
            higher than the motor or driver can handle."""
            */
        public void SetStepperResolution(int microsteps, int minMicrostepDuration = 1000)
        {
            microSteps = microsteps;
            this.minMicrostepDuration = minMicrostepDuration;
            UpdateStepperSpeed();
        }
        //"""Set the stepper target position. The target position is in 1/256 of
        // a whole step. (So setting the target to 256 will take as many steps /
        //   microsteps as are necessary to move to the position that is 1 whole
        //   step from the starting position."""
        public void SetStepperTarget(int targetPos)
        {
            byte[] data = new byte[] {  (byte)(targetPos & 0xFF),
                                    (byte)((targetPos >> 8) & 0xFF),
                                    (byte)((targetPos >> 16) & 0xFF),
                                    (byte)((targetPos >> 24) & 0xFF) };

            transfer(kFunctionSetStepperTarget, data, 0);
        }
        /*    """Return the current position of the stepper motor in 1/256 increments
               of wholes steps."""
        */
        public int GetStepperPosition()
        {
            byte[] receiveData = transfer(kFunctionGetStepperPosition, null, 4);
            int pos = 0;
            if (receiveData != null)
            {
                for (int i = 3; i >= 0; i++)
                {
                    pos = (pos << 8);
                    pos = pos | receiveData[i];
                }
            }
            return pos;
        }
        //"""Return whether a fault condition (such as a short between motor terminals,
        //over current shutdown, or over temperature shutdown) is currently present."""
        public bool HasFault()
        {
            return fault;
        }
        public void UpdateStepperSpeed()
        {
            //# Find the actual number of microsteps to use. If the duration of a
            //# microstep would be less than _minMicrostepDuration, then this will
            //# be less than the requested number of microsteps.
            int microsteps = microSteps;

            if (microsteps > 256)
            {
                microsteps = 256;
            }

            while (microsteps > 1 && usPerStep / microsteps < minMicrostepDuration)
            {
                microsteps /= 2;
            }

            //# Now determine the microstep resolution, which is log2(microsteps)
            int resolution = 0;
            int i = microsteps / 2;
            while (i > 0 && resolution <= 8)
            {
                resolution += 1;
                i /= 2;
            }

            //# Determine the number of 8us ticks per microstep
            int ticksPerMicrostep = (usPerStep / (8 * microsteps)).Clamp(0, 65535);

            byte[] sendData = new byte[] { (byte)(ticksPerMicrostep & 0xFF), (byte)(ticksPerMicrostep >> 8), (byte)resolution };
            transfer(kFunctionSetStepperSpeed, sendData, 0);
        }
        public override void ProcessEvent(byte code, ushort data)
        {
            if (code == kEventPositionReached)
            {
                if (positionReachedCallback != null)
                {
                    positionReachedCallback(this);
                }
            }

            if (code == kEventFaultChanged)
            {
                if((data & 1) != 0)
                {
                    fault = true;
                    if (faultChangedCallback != null)
                    {
                        faultChangedCallback(this);
                    }
                }
                if((data & 2) != 0)
                {
                    fault = false;
                    if (faultChangedCallback != null)
                    {
                        faultChangedCallback(this);
                    }
                }
            }
        }
    }
}
