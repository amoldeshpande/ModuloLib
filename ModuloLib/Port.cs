using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ModuloLib
{
    public class Port
    {
        const byte kBroadcastAddress = 9;
        const byte kBroadcastCommandGlobalReset = 0;
        const byte kBroadcastCommandGetNextDeviceID = 1;

        const byte kBroadcastCommandGetNextUnassignedDeviceID = 2;
        const byte kBroadcastCommandSetAddress = 3;
        const byte kBroadcastCommandGetAddress = 4;
        const byte kBroadcastCommandGetDeviceType = 5;
        const byte kBroadcastCommandGetVersion = 6;
        const byte kBroadcastCommandGetEvent = 7;
        const byte kBroadcastCommandClearEvent = 8;
        const byte kBroadcastCommandSetStatusLED = 9;

        const byte kBroadcastCommandExitBootloader = 100;

        static readonly byte kCodeEvent = Encoding.ASCII.GetBytes("V")[0];
        static readonly byte kCodeEcho =  Encoding.ASCII.GetBytes("X")[0];

        const int kStatusOff = 0;
        const int kStatusOn = 1;
        const int kStatusBlinking = 2;

        public byte LastAssignedAddress{get; set;} = 9;
        SerialConnection connection;
        public SerialConnection Connection { get { return connection; } }
        List<Modulo> modulos = new List<Modulo>();

        public async Task<bool> Initialize(String portName)
        {
            connection = new SerialConnection();
            return await connection.Initialize(portName);
        }  
        public void Close()
        {
            connection.Close();
            connection.Dispose();
            connection = null;
        }
        internal void AddModulo(Modulo toAdd)
        {
            modulos.Add(toAdd);
        }      
        internal void RemoveModulo(Modulo toRemove)
        {
            modulos.Remove(toRemove);
        }
        internal Modulo findModuloById(ushort deviceId)
        {
           return (from m in modulos
             where m.DeviceId == deviceId
             select m).FirstOrDefault();
        }
        public void RunForever()
        {
            while(Loop())
            {
            }
        }
        public bool Loop(bool noWait = false)
        {
            foreach(Modulo m in modulos)
            {
               m.GetAddress();
            }
            byte[] packet = connection.getNextPacket();
            while(packet != null)
            {
                if(packet[0] == kCodeEvent)
                {
                    int eventIndex = 1;
                    byte eventCode = packet[eventIndex];
                    ushort deviceId = (ushort)(packet[eventIndex + 1] | (packet[eventIndex + 2] << 8));
                    ushort eventData = (ushort)(packet[eventIndex + 3] | (packet[eventIndex + 4] << 8));
                    Modulo m = findModuloById(deviceId);
                    if(m != null)
                    {
                        m.ProcessEvent(eventCode, eventData);
                        if(m.ShouldExit)
                        {
                            return false;
                        }
                    }
                }
                else if(packet[0] != kCodeEcho)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid packet " + packet[0].ToString("X"));
                }
                packet =  connection.getNextPacket(true);
            }
            return true;
        }
        private void globalReset()
        {
            //"""Reset all modulos to their initial state"""
            connection.transfer(kBroadcastAddress, kBroadcastCommandGlobalReset, null, 0);

            foreach (Modulo m in modulos)
            {
                m.Reset();
            }
        }
        private void exitBootloader()
        {
            connection.transfer(kBroadcastAddress, kBroadcastCommandExitBootloader, null, 0);
        }
        private ushort? getDeviceId(ushort lastDeviceId,byte command)
        {
            if (lastDeviceId == 0xFFFF)
            {
                return 0xFFFF;
            }
            ushort nextDeviceID = (ushort)(lastDeviceId + 1);
            byte[] sendData = new byte[] { (byte)(nextDeviceID & 0xFF), (byte)(nextDeviceID >> 8) };
            byte[] resultData = connection.transfer(kBroadcastAddress, command, sendData, 2);
            if (resultData != null)
            {
                return (ushort)(resultData[1] | resultData[0] << 8);
            }
            return null;
        }
        public ushort? GetNextDeviceID(ushort lastDeviceId)
        {
            return getDeviceId(lastDeviceId, kBroadcastCommandGetNextDeviceID);
        }
        private ushort? getNextUnassignedDeviceID(ushort lastDeviceId)
        {
            return getDeviceId(lastDeviceId, kBroadcastCommandGetNextUnassignedDeviceID);            
        }
        internal void setAddress(ushort deviceId,byte address)
        {
            byte[] sendData = new byte[] { (byte)(deviceId & 0xFF), (byte)(deviceId >> 8), address };
            connection.transfer(kBroadcastAddress, kBroadcastCommandSetAddress, sendData, 0);
        }
        internal byte? getAddress(ushort deviceId)
        {
            byte[] sendData = new byte[] { (byte)(deviceId & 0xFF), (byte)(deviceId >> 8)};
            byte[] result = connection.transfer(kBroadcastAddress, kBroadcastCommandGetAddress, sendData, 1);
            if(result != null)
            {
                return result[0];
            }
            return null;
        }
        private void setStatus(ushort deviceId, byte status)
        {
            byte[] sendData = new byte[] { (byte)(deviceId & 0xFF), (byte)(deviceId >> 8), status };
            connection.transfer(kBroadcastAddress, kBroadcastCommandSetStatusLED, sendData, 0);
        }
        private ushort? getVersion(ushort deviceId)
        {
            byte[] sendData = new byte[] { (byte)(deviceId & 0xFF), (byte)(deviceId >> 8) };
            byte[] result = connection.transfer(kBroadcastAddress, kBroadcastCommandGetVersion, sendData, 2);
            if (result != null)
            {
                return result[0];
            }
            return null;
        }
        public String GetDeviceType(ushort deviceId)
        {
            byte[] sendData = new byte[] { (byte)(deviceId & 0xFF), (byte)(deviceId >> 8) };
            byte[] result = connection.transfer(kBroadcastAddress, kBroadcastCommandGetDeviceType, sendData, 31);
            if (result != null)
            {
                String res =  Encoding.ASCII.GetString(result).TrimEnd(new char[] { '\0' }) ;
                if (res.IndexOf('\0') > 0)
                {
                    return res.Substring(0, res.IndexOf('\0'));
                }
                return res;
            }
            return String.Empty;
        }
    }
}
