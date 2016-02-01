using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace ModuloLib
{
    public class SerialConnection : IDisposable
    {
        enum PacketState
        {
            WaitForInitialDelimiter,
            WaitForFinalDelimiter,
            EscapeNextByte
        }
        class Packet
        {
            public int ByteCount { get; private set; }
            public PacketState State = PacketState.WaitForInitialDelimiter;
            MemoryStream data = new MemoryStream();
            public void AddByte(byte b)
            {
                ByteCount++;
                data.WriteByte(b);
            }
            public byte[] GetData()
            {
                return data.ToArray();
            }
            public DateTime receivedFromPortStart;
            public DateTime receivedFromPortEnd;
            public int GetMSInQueue()
            {                
                TimeSpan t = DateTime.Now - receivedFromPortEnd;
                if (receivedFromPortEnd != receivedFromPortStart)
                {
                    Debug.WriteLine("Time spent in queue " + (receivedFromPortEnd - receivedFromPortStart).TotalMilliseconds);
                }
                return (int)t.TotalMilliseconds;
            }
        }
        const byte kDelimiter = 0x7E;
        const byte kEscape = 0x7D;
        static readonly byte kCodeEcho = Encoding.ASCII.GetBytes("X")[0];
        static readonly byte kCodeTransfer = Encoding.ASCII.GetBytes("T")[0];
        static readonly byte kCodeReceive = Encoding.ASCII.GetBytes("R")[0];
        static readonly byte kCodeQuit = Encoding.ASCII.GetBytes("Q")[0];
        const ushort kModuloVendorId = 0x16d0;
        const ushort kModuloProductId = 0x0b58;
        const int kBaudRate = 9600;
        const int kReadBufferSize = 64;

        SerialDevice serialDevice = null;

        BufferBlock<Packet> incomingPacketQueue = new BufferBlock<Packet>();
        Task receiveTask;
        CancellationTokenSource readCancellation = new CancellationTokenSource();
        Task sendTask;

        BufferBlock<byte[]> outgoingBufferQueue = new BufferBlock<byte[]>(
                                                    new DataflowBlockOptions { BoundedCapacity = 30 });

        Queue<byte[]> outOfBandPackets = new Queue<byte[]>();

        public async Task<bool> Initialize(String port = null, int controller = 0)
        {
            String aqsFilter;
            if (port == null)
            {
                aqsFilter = SerialDevice.GetDeviceSelectorFromUsbVidPid(kModuloVendorId, kModuloProductId);
            }
            else
            {
                aqsFilter = SerialDevice.GetDeviceSelector(port);
            }
            var devices = await DeviceInformation.FindAllAsync(aqsFilter);
            foreach (DeviceInformation device in devices)
            {
                System.Diagnostics.Debug.WriteLine("Device " + device.Name);
                if (device.Name.Contains("Modulo") == false)
                {
                    continue;
                }
                serialDevice = await SerialDevice.FromIdAsync(device.Id);
                break;
            }
            if (serialDevice == null)
            {
                return false;
            }
            serialDevice.BaudRate = kBaudRate;
            serialDevice.ReadTimeout =  TimeSpan.FromMilliseconds(100);
            serialDevice.WriteTimeout = TimeSpan.FromSeconds(1);
            serialDevice.IsDataTerminalReadyEnabled = true;

            receiveTask = new Task(deviceReader);
            receiveTask.Start();
            sendTask = new Task(deviceWriter);
            sendTask.Start();
            do
            {
               sendPacket(new byte[] { kCodeEcho });
            } while ((getNextPacket()) == null);
            return true;
        }
        public void Dispose()
        {
            if (serialDevice != null)
            {
                serialDevice.Dispose();
            }
        }
        public void Close()
        {
            sendPacket(new byte[] { kCodeEcho });
            incomingPacketQueue.Complete();
            outgoingBufferQueue.Complete();
            readCancellation.Cancel();            
            receiveTask.Wait();
            sendTask.Wait();
            return;
        }       
        internal byte[] transfer(byte address, byte command, byte[] sendData, byte receiveLen)
        {           
            using (MemoryStream ms = new MemoryStream())
            {
                int n = (sendData != null ? sendData.Length : 0);
                ms.WriteByte(kCodeTransfer);
                ms.WriteByte(address);
                ms.WriteByte(command);
                ms.WriteByte((byte)n);
                ms.WriteByte(receiveLen);
                if (n != 0)
                {
                    ms.Write(sendData, 0, sendData.Length);
                }
               sendPacket(ms.ToArray());
            }
            if(receiveLen == 0)
            {
                return null;
            }
            byte[] packet = null;
            int retries = 0;
           // while (packet == null)
            {
                packet = receivePacket();
                while(packet != null && packet[0] != kCodeReceive)
                {
                    outOfBandPackets.Enqueue(packet);
                    packet = receivePacket();
                    retries++;
                }
            }            
            if (packet == null || packet.Length == 2)
            {
                return null;
            }
            byte[] data = new byte[packet.Length - 2];
            Array.Copy(packet, 2, data, 0, data.Length);
            return data;
        }
        private void sendPacket(byte[] data)
        {
            outgoingBufferQueue.Post(data);
        }
        internal byte[] receivePacket(bool noWait = false)
        {            
            Packet p = null;
            if (incomingPacketQueue.TryReceive(out p))
            {
               // Debug.WriteLine("ms in q " + p.GetMSInQueue());
                return p.GetData();
            }
            if(noWait)
            {
                return null;
            }
            int tries = 20;
            do
            {
                Task.Delay(5).Wait();
                if (incomingPacketQueue.TryReceive(out p))
                {
                    // Debug.WriteLine("ms in q " + p.GetMSInQueue());
                    return p.GetData();
                }
            } while (--tries > 0);

            return null;
        }
        internal byte[] getNextPacket(bool noWait = false)
        {
            if(outOfBandPackets.Count > 0)
            {
                return outOfBandPackets.Dequeue();
            }
            if (incomingPacketQueue.Count == 0 && noWait)
            {
                return null;
            }
            return receivePacket();
        }
        private void deviceWriter()
        {
            Task x = deviceWriterAsync();
            x.Wait();
        }
        private async Task deviceWriterAsync()
        {            
            while (true)
            {
                try
                {
                    byte[] data = outgoingBufferQueue.Receive();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        foreach (byte b in data)
                        {
                            if (b == kDelimiter || b == kEscape)
                            {
                                ms.WriteByte(kEscape);
                                ms.WriteByte((byte)(b ^ (1 << 5)));
                            }
                            else
                            {
                                ms.WriteByte(b);
                            }
                        }
                        using (DataWriter dataWriter = new DataWriter(serialDevice.OutputStream))
                        {
                            dataWriter.WriteByte(kDelimiter);
                            dataWriter.WriteBytes(ms.ToArray());
                            dataWriter.WriteByte(kDelimiter);
                            try
                            {
                                await dataWriter.StoreAsync();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("write timeout. " + ex.ToString());
                            }
                            finally
                            {
                                dataWriter.DetachStream();
                            }
                        }
                    }
                }
                catch(InvalidOperationException ioe)
                {
                    System.Diagnostics.Debug.WriteLine("Invalidop exception quitting writer thread " + ioe.ToString());
                    break;
                }
            }
        }
        private void deviceReader()
        {
            Task x = deviceReaderAsync();
            x.Wait();
        }
        private async Task deviceReaderAsync()
        {
            Packet currentPacket = new Packet();

            using (DataReader dataReader = new DataReader(serialDevice.InputStream))
            {
                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                try
                {
                    while (true)
                    {
                        await dataReader.LoadAsync(kReadBufferSize).AsTask(readCancellation.Token);
                        while (dataReader.UnconsumedBufferLength > 0)
                        {
                            if (currentPacket.ByteCount == 0)
                            {
                                currentPacket.receivedFromPortStart = DateTime.Now;
                            }
                            if (processByte(currentPacket, dataReader.ReadByte()))
                            {
                                currentPacket.receivedFromPortEnd = DateTime.Now;
                                //if it's an empty receive, drop it.
                                if(currentPacket.ByteCount == 2)
                                {
                                    if (currentPacket.GetData()[0] == kCodeReceive)
                                    {
                                        currentPacket = new Packet();
                                        continue;
                                    }
                                }
                                incomingPacketQueue.Post(currentPacket);
                                currentPacket = new Packet();
                            }
                        }
                    }
                }
                catch (TaskCanceledException tce)
                {
                    System.Diagnostics.Debug.WriteLine("Read cancelled " + tce.ToString());
                }
                finally
                {
                    dataReader.DetachStream();
                }

            }
        }
        private bool processByte(Packet p, byte b)
        {
            if (b != kDelimiter && p.State == PacketState.WaitForInitialDelimiter)
            {
                return false;
            }
            if (b == kDelimiter)
            {
                if(p.State == PacketState.WaitForInitialDelimiter)
                {
                    p.State = PacketState.WaitForFinalDelimiter;
                }
                else if(p.State == PacketState.WaitForFinalDelimiter && p.GetData().Length != 0)
                {
                    return true;
                }
                return false;
            }
            if (b == kEscape)
            {
                p.State = PacketState.EscapeNextByte;
                return false;
            }

            if (p.State == PacketState.EscapeNextByte)
            {
                p.AddByte((byte)(b ^ (1 << 5)));
                p.State = PacketState.WaitForFinalDelimiter;
            }
            else
            {
                p.AddByte(b);
            }
            return false;
        }
        
    }
}
