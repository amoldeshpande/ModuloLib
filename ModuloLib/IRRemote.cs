using System;
using System.IO;
using System.Threading.Tasks;

namespace ModuloLib
{
    public class IRRemote : Modulo
    {
        const byte kFUNCTION_RECEIVE = 0;
        const byte kFUNCTION_GET_READ_SIZE = 1;
        const byte kFUNCTION_CLEAR_READ = 2;
        const byte kFUNCTION_SET_SEND_DATA = 3;
        const byte kFUNCTION_SEND = 4;
        const byte kFUNCTION_IS_IDLE = 5;
        const byte kFUNCTION_SET_BREAK_LENGTH = 6;

        const byte kEVENT_RECEIVE = 0;

        Action<IRRemote,sbyte,int, ushort[],int> dataReceivedCallback = null;

        public bool Initialize(Port port, 
                               ushort? deviceId = null,
                               Action<IRRemote,sbyte,int, ushort[],int> drc = null)
        {
            dataReceivedCallback = drc;
            return base.Initialize(port, "co.modulo.ir", deviceId);
        }
        public void SetBreakLength(ushort len)
        {
            transfer(kFUNCTION_SET_BREAK_LENGTH, new byte[] { (byte)(len & 0xFF), (byte)(len >> 8) },0);
        }
        public override void ProcessEvent(byte code, ushort eventData)
        {
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                int i = 0;
                while (i < eventData)
                {
                    byte[] recvd = transfer(kFUNCTION_RECEIVE, new byte[] { (byte)i, (byte)16 }, 16);
                    if (recvd != null)
                    {
                        ms.Write(recvd, 0, recvd.Length);
                        i += recvd.Length;
                    }
                }
                transfer(kFUNCTION_CLEAR_READ, null, 0);
                data = ms.ToArray();
            }
            ushort[] expandedData = new ushort[data.Length];
            ushort expandedLen = 0;
            for (int i = 0; i < data.Length; i++)
                {
                    ushort value = 0;
                    if ( data[i] == 0 && ( i+2 < data.Length) ) {
                    value = (ushort)( data[i + 1] + (data[i + 2] << 8));
                    i += 2;
                }
                else
                {
                    value = data[i];
                }

                // Due to sensor lag, each mark tends to be approx 1 tick long and
                // each space tends to be 1 tick short. Adjust for that here.
                /*
                        if (expandedLen%2) {
                            value--;
                        } else {
                            value++;
                        }
                */

                expandedData[expandedLen++] = value;

            }
            // If the length is less than 2, it's a surpious signal that should be ignored.
            if (expandedLen <= 2)
            {
                return;
            }
            {
                sbyte protocol = -1;
                uint value = 0;
                IREncoding encoder = new IREncoding();
                encoder.IRDecode(expandedData, expandedLen, ref protocol, ref value);

                if (dataReceivedCallback != null)
                {
                    dataReceivedCallback(this,protocol, (int)value, expandedData, expandedLen);
                }
            }
        }
        
        public void Send(byte protocol, uint data)
        {

            byte[] rawData = new byte[128];
            IREncoding encoder = new IREncoding();

            int rawLen = encoder.IREncode(protocol, data, rawData, 128);

            if (rawLen > 0)
            {
                SendRaw(rawData);
            }
        }

        public void SendRaw(byte[] data)
        {
            bool isIdle = false;
            while(!isIdle)
            {
                byte[] val = transfer(kFUNCTION_IS_IDLE, null, 1);
                if(val == null)
                {
                    return;
                }
                isIdle = (val[0] == 0 ? false : true);
                if(isIdle)
                {
                    Task.Delay(5).Wait();
                }
            }
            for(int i =0; i < data.Length;i+= 16)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteByte((byte)i);
                    ms.Write(data, i, 16);
                    if(transfer(kFUNCTION_SET_SEND_DATA,ms.ToArray(),0) == null)
                    {
                        return;
                    }
                }
            }
            transfer(kFUNCTION_SEND, new byte[] { (byte)data.Length }, 0);
        }
    }
}
