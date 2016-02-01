using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    public class Joystick : Modulo
    {
        const byte FUNCTION_GET_BUTTON = 0;
        const byte FUNCTION_GET_POSITION = 1;

        const byte EVENT_BUTTON_CHANGED = 0;
        const byte EVENT_POSITION_CHANGED = 1;

        bool buttonState;
        public bool GetButtonState() { return buttonState; }
        byte hPos;
        byte vPos;
        Action<Modulo> buttonPressCallback = null;
        Action<Modulo> buttonReleaseCallback = null;
        Action<Modulo> positionChangeCallback = null;

        public bool Initialize(Port port,
                                  ushort? deviceId = null,
                                  Action < Modulo > btnPressCallback = null,
                                  Action<Modulo> btnReleaseCallback = null,
                                  Action<Modulo> psnChangeCallback = null)
        {
            base.Initialize(port, "co.modulo.joystick", deviceId);
            buttonState = false;
            hPos = vPos = 128;
            buttonPressCallback = btnPressCallback;
            buttonReleaseCallback = btnReleaseCallback;
            positionChangeCallback = psnChangeCallback;
            return true;
        }
        public float GetHPos()
        {
            init();
            return (1.0f - hPos * 2.0f / 255.0f);
        }
        public float GetVPos()
        {
            init();
            return (1.0f - vPos * 2.0f / 255.0f);
        }
        protected override bool init()
        {
            if (base.init())
            {
                refreshState();
                return true;
            }
            return false;
        }
        private void refreshState()
        {
            byte[] receivedData = transfer(FUNCTION_GET_BUTTON, null, 1);
            if (receivedData != null)
            {
                buttonState = (receivedData[0] == 1 ? true : false);
            }
            receivedData = transfer(FUNCTION_GET_POSITION, null, 2);
            if (receivedData != null)
            {
                hPos = receivedData[0];
                vPos = receivedData[1];
            }
        }
        public override void ProcessEvent(byte code, ushort data)
        {
            if (code == EVENT_BUTTON_CHANGED)
            {
                bool buttonPressed = ((data >> 8) == 0 ? false : true);
                bool buttonReleased = ((data & 0xFF) == 0 ? false : true);

                buttonState = buttonState || buttonPressed;
                buttonState = buttonState && !buttonReleased;

                if (buttonPressed && buttonPressCallback != null)
                {
                    buttonPressCallback(this);
                }
                if (buttonReleased && buttonReleaseCallback != null)
                {
                    buttonReleaseCallback(this);
                }
            }
            else if (code == EVENT_POSITION_CHANGED)
            {
                hPos = (byte)(data >> 8);
                vPos = (byte)(data & 0xFF);
                if(positionChangeCallback != null)
                {
                    positionChangeCallback(this);
                }
            }
        }
    }   
}
