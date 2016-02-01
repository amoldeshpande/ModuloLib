using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    public class Knob : Modulo
    {
        const byte kFunctionGetButton = 0;
        const byte kFunctionGetPosition = 1;
        const byte kFunctionAddOffsetPosition = 2;
        const byte kFunctionSetColor = 3;

        const byte kEventButtonChanged = 0;
        const byte kEventPositionChanged = 1;

        bool buttonState;
        internal bool GetButtonState() { return buttonState; }
        short position;
        internal short GetPosition() { return position; }
        Action<Modulo> buttonPressCallback = null;
        Action<Modulo> buttonReleaseCallback = null;
        Action<Modulo> positionChangeCallback = null;

        public bool Initialize(Port port, 
                               ushort? deviceId = null,
                               Action<Modulo> btnPressCallback = null,
                               Action<Modulo> btnReleaseCallback = null,
                               Action<Modulo> psnChangeCallback = null)
        {
            base.Initialize(port, "co.modulo.knob", deviceId);
            buttonState = false;
            position = 0;
            buttonPressCallback = btnPressCallback;
            buttonReleaseCallback = btnReleaseCallback;
            positionChangeCallback = psnChangeCallback;
            return true;
        }
        
        public void SetColor(float red,float green, float blue)
        {
            byte[] sendData = new byte[] { (byte)(red * 255), (byte)(green * 255), (byte)(blue * 255) };
            transfer(kFunctionSetColor, sendData, 0);
        }
        public int GetAngle()
        {
            return (position % 24) * 15;
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
            byte[] receivedData = transfer(kFunctionGetPosition, null, 2);
            if(receivedData != null)
            {
                position = (short)(receivedData[0] | receivedData[1] << 8);
            }
            receivedData = transfer(kFunctionGetButton, null, 1);
            if(receivedData != null)
            {
                buttonState = (receivedData[0] == 1 ? true : false);
            }
            return;
        }
        public override void ProcessEvent(byte code, ushort data)
        {
            if(code == kEventButtonChanged)
            {
                bool buttonPressed = ((data & 0x0100) == 0 ? false : true);
                bool buttonReleased = ((data & 0x0001) == 0 ? false : true);

                buttonState = buttonState || buttonPressed;
                buttonState = buttonState && !buttonReleased;

                if(buttonPressed && buttonPressCallback != null)
                {
                    buttonPressCallback(this);
                }
                if(buttonReleased && buttonReleaseCallback != null)
                {
                    buttonReleaseCallback(this);
                }
            }
            else if(code == kEventPositionChanged)
            {
                position = (short)data;
                if(positionChangeCallback != null)
                {
                    positionChangeCallback(this);
                }
            }
        }        
    }

}
