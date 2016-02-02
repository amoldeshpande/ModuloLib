/*
 *  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    public class DisplayModulo : Modulo
    {
        const byte FUNCTION_APPEND_OP = 0;
        const byte FUNCTION_IS_COMPLETE = 1;
        const byte FUNCTION_GET_BUTTONS = 2;
        const byte FUNCTION_RAW_WRITE = 3;
        const byte FUNCTION_IS_EMPTY = 4;
        const byte FUNCTION_GET_AVAILABLE_SPACE = 5;
        const byte FUNCTION_SET_CURRENT = 6;
        const byte FUNCTION_SET_CONTRAST = 7;

        const byte EVENT_BUTTON_CHANGED = 0;

        const byte OpRefresh = 0;
        const byte OpFillScreen = 1;
        const byte OpDrawLine = 2;
        const byte OpSetLineColor = 3;
        const byte OpSetFillColor = 4;
        const byte OpSetTextColor = 5;
        const byte OpDrawRect = 6;
        const byte OpDrawCircle = 7;
        const byte OpDrawTriangle = 8;
        const byte OpDrawString = 9;
        const byte OpSetCursor = 10;
        const byte OpSetTextSize = 11;
        const byte OpClear = 12;

        const byte OP_BUFFER_SIZE = 28;

        public byte Width { get; private set; }
        public byte Height { get; private set; }
        ushort availableSpace = 0;
        byte opBufferLen = 0;
        sbyte currentOp = -1;
        byte[] opBuffer = new byte[OP_BUFFER_SIZE];
        bool buttonState = false;
        bool isRefreshing = false;


        Action<Modulo,int> buttonPressCallback = null;
        Action<Modulo,int> buttonReleaseCallback = null;

        public bool Initialize(Port port, 
                               ushort? deviceId = null,
                               Action<Modulo,int> pressCallback = null,
                               Action<Modulo,int> releaseCallback = null)
        {
            base.Initialize(port, "co.modulo.display", deviceId);
            Width = 96;
            Height = 64;
            buttonPressCallback = pressCallback;
            buttonReleaseCallback = releaseCallback;
            return true;
        }
        protected void sendOp(byte[] data)
        {
            while (availableSpace < data.Length)
            {
                byte[] receiveData = transfer(FUNCTION_GET_AVAILABLE_SPACE, null, 2);
                if (receiveData != null)
                {
                    availableSpace = (ushort)(receiveData[0] | (receiveData[1] << 8));
                    if (availableSpace < data.Length)
                    {
                        Task.Delay(5).Wait();
                    }
                }
            }
            availableSpace -= (ushort)data.Length;
            transfer(FUNCTION_APPEND_OP, data, 0);
        }
        protected void beginOp(byte opcode)
        {
            if (opcode == currentOp)
            {
                return;
            }
            currentOp = (sbyte)opcode;
            opBufferLen = 1;
            opBuffer[0] = (byte)opcode;
        }
        protected void appendToOp(byte data)
        {
            opBuffer[opBufferLen] = data;
            opBufferLen += 1;
            if (currentOp == OpDrawString && opBufferLen == OP_BUFFER_SIZE - 1)
            {
                endOp();
            }
        }
        protected void endOp()
        {
            if (currentOp == OpDrawString)
            {
                opBuffer[opBufferLen] = 0;
                opBufferLen += 1;
                byte[] dataToSend = new byte[opBufferLen];
                Array.Copy(opBuffer, 0, dataToSend, 0, opBufferLen);
                sendOp(dataToSend);
                opBufferLen = 0;
                currentOp = -1;
            }
        }
        public void Clear()
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpClear });
        }
        protected void setColor(byte op,float r, float g, float b, float a = 1)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { op,
                                       (byte)(255*r.Clamp(0,1)),
                                       (byte)(255*g.Clamp(0,1)),
                                       (byte)(255*b.Clamp(0,1)),
                                       (byte)(255*a.Clamp(0,1)),
                                    });
        }
        public void SetLineColor(float r, float g, float b, float a = 1)
        {
            setColor(OpSetLineColor,r,g,b,a);
        }
        public void SetLineColor(params float[] rgba)
        {
            SetLineColor(rgba[0], rgba[1], rgba[2], rgba[3]);
        }
        public void  SetFillColor(float r, float g, float b, float a = 1)
        {
            setColor(OpSetFillColor, r, g, b, a);
        }
        public void SetFillColor(params float[] rgba)
        {
            SetFillColor(rgba[0], rgba[1], rgba[2], rgba[3]);
        }
        public void setTextColor(float r, float g, float b, float a = 1)
        {
            setColor(OpSetTextColor, r, g, b, a);
        }
        public void SetTextColor(params float[]rgba)
        {
            setTextColor(rgba[0], rgba[1], rgba[2], rgba[3]);
        }
        public void SetCursor(int x, int y)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpSetCursor, (byte)x, (byte)y });
        }
        public void   Refresh(bool flip = false)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpRefresh, flip ? (byte)1 : (byte)0 });
            isRefreshing = true;
        }
        protected void   fillScreen(float r,float g, float b)
        {
            endOp();
            waitOnRefresh();
            setColor(OpFillScreen, r, g, b, 255);
        }       
        protected void drawLine(byte x0, byte y0, byte x1, byte y1)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpDrawLine, x0, y0, x1, y1 });
        }
        private Tuple<byte,byte> clipRange(sbyte x, sbyte w, sbyte maxWidth)
        {
            sbyte left = -128;
            if(x < left)
            {
                w += (sbyte)(x - left);
                x = left;
            }
            if(w<= 0 || x >= maxWidth)
            {
                return new Tuple<byte, byte>(0, 0);
            }
            if((byte)w > 255)
            {
                w = -1 ;
            }

            return new Tuple<byte, byte>((byte)x, (byte)w);
        }
        public void   DrawRect(byte x, byte y, byte w, byte h, byte r = 0)
        {
            endOp();
            waitOnRefresh();
            var xw = clipRange((sbyte)x, (sbyte)w, (sbyte)Width);
            var yh = clipRange((sbyte)y, (sbyte)h, (sbyte)Height);

            sendOp(new byte[] { OpDrawRect, xw.Item1,  yh.Item1, xw.Item2, yh.Item2, r });
        }
        protected void  drawTriangle(sbyte x0,sbyte y0,sbyte x1, sbyte y1, sbyte x2, sbyte y2)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpDrawTriangle, (byte)x0, (byte)y0, (byte)x1, (byte)y1, (byte)x2, (byte)y2 });
        }
        public void  DrawCircle(sbyte x, sbyte y, byte radius)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpDrawCircle, (byte)x, (byte)y, radius });
        }
        public void  Write(String s)
        {
            waitOnRefresh();
            if(currentOp != OpDrawString)
            {
                endOp();
                beginOp(OpDrawString);
            }
            foreach(byte c in Encoding.ASCII.GetBytes(s))
            {
                appendToOp(c);
            }
        }
        private void  setTextSize(byte size)
        {
            endOp();
            waitOnRefresh();
            sendOp(new byte[] { OpSetTextSize, size });
        }
        private bool isComplete()
        {
            byte[] result = transfer(FUNCTION_IS_COMPLETE, null, 1);
            return (result != null && result[0] != 0);
        }
        private bool isEmpty()
        {
            byte[] result = transfer(FUNCTION_IS_EMPTY, null, 1);
            return (result != null && result[0] != 0);
        }
        private void waitOnRefresh()
        {
            if(isRefreshing)
            {
                isRefreshing = false;
                while(!(isEmpty()))
                {
                    Task.Delay(5).Wait();
                }
            }
        }
        private bool getButton(int button)
        {
            return (((getButtons()) & (1 << button)) != 0);
        }
        private byte getButtons()
        {
            byte[] receiveData = transfer(FUNCTION_GET_BUTTONS, null, 1);
            if(receiveData == null)
            {
                return 0;
            }
            return receiveData[0];
        }
        private void setContrast(float r, float g, float b)
        {
            byte[] contrast = new byte[] {FUNCTION_SET_CONTRAST,
                                          (byte)(255*r.Clamp(0,1)),
                                          (byte)(255*g.Clamp(0,1)),
                                          (byte)(255*g.Clamp(0,1)) };

            while (!(isComplete()))
            {
                Task.Delay(5).Wait();
            }
            transfer(FUNCTION_SET_CONTRAST, contrast, 0);
        }
        public override void ProcessEvent(byte code, ushort data)
        {
            if(code == EVENT_BUTTON_CHANGED)
            {
                int buttonPressed = (data >> 8) ;
                int buttonReleased = (data & 0xFF);

                buttonState = (buttonPressed == 0 ? false : true);
                buttonState = (buttonState && (buttonReleased != 0));
                for(int i =0; i< 3; i++)
                {
                    if((buttonPressed & (1<< i) ) != 0 && buttonPressCallback != null)
                    {
                        buttonPressCallback(this,i);
                    }
                    if ((buttonReleased & (1 << i)) != 0 && buttonReleaseCallback != null)
                    {
                        buttonReleaseCallback(this,i);
                    }
                }
            }
        }
    }

}