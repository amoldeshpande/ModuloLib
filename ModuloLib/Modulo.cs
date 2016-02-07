using System;
using System.Threading.Tasks;

namespace ModuloLib
{
    public abstract class Modulo : IDisposable
    {
        Port port;
        ushort? deviceId;
        public ushort DeviceId { get { return deviceId.HasValue ? deviceId.Value : (ushort)0; } }
        String deviceType;        
        byte? address;

        public bool ShouldExit { get; set; } = false;

        protected virtual bool Initialize(Port prt, String devType, ushort? devId)
        {
            port = prt;
            deviceType = devType;
            deviceId = devId;
            port.AddModulo(this);
            return true;
        }
        public void Dispose()
        {
            close();
        }
        private void close()
        {
            if(port != null)
            {
                port.RemoveModulo(this);
            }
        }
        internal byte[] transfer(byte command, byte[] sendData, byte receiveLen)
        {
            byte? addr = GetAddress();
            if(!addr.HasValue)
            {
                return null;
            }
            return port.Connection.transfer(addr.Value, command, sendData, receiveLen);
        }
        public void Reset()
        {
            address = null;
        }
        public virtual void ProcessEvent(byte code, ushort data)
        {

        }
        public ushort? GetDeviceId()
        {
            init();
            return deviceId;
        }
        public void SetDeviceId(ushort devcId)
        {
            if(deviceId != devcId)
            {
                deviceId = devcId;
                address = null;
            }
        }
        public byte? GetAddress()
        {
            init();
            return address;
        }
        protected void  loop()
        {
            
        }
        protected virtual bool init()
        {
            if(address != null)
            {
                return false;
            }
            if(deviceId == null)
            {
                ushort? devcId = port.GetNextDeviceID(0);
                while(devcId != null)
                {
                    Modulo m = port.findModuloById(devcId.Value);
                    if( m == null)
                    {
                        String dt = port.GetDeviceType(devcId.Value);
                        if ( dt  == deviceType)
                        {
                            deviceId = devcId;
                            break;
                        }
                    }
                    devcId = port.GetNextDeviceID(devcId.Value);
                }
            }
            if(deviceId == null)
            {
                return false;
            }
            address = port.getAddress(deviceId.Value);
            if(address == 0 || address == 127)
            {
                port.LastAssignedAddress += 1;
                address = port.LastAssignedAddress;
                port.setAddress(deviceId.Value, address.Value);
            }
            return true;
        }
    }
    public static class MathHelper
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
