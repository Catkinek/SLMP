using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace PierwszyProgramSLMP
{
    class Program
    {
        static TcpClient tcpClient = new TcpClient();
        static void Main(string[] args)
        {
            
            IPAddress iPAddress = new IPAddress(new byte[] { 192, 168, 3, 250 });
            ConnectTCP(iPAddress, 2000);
            Communication comm = new Communication(tcpClient);
            if (tcpClient.Connected)
            {
                Console.WriteLine("Połączono z PLC");
                //Communication.ReadValue registers = comm.ReadDataBatch(VarTypes.Register, 200, 20);
                //Communication.ReadValue alarms = comm.ReadDataBatch(VarTypes.Alarm, 0, 2);
                Communication.ReadValueBit alarmsBit = comm.ReadDataBatchBit(VarTypes.Alarm, 0, 7);
                if (alarmsBit.successRead) 
                { 
                    foreach (var item in alarmsBit.value)
                    {
                        Console.WriteLine(item);
                    }
                }
                //comm.ReadDataBatch(VarTypes.Register, 200, 20);
                //comm.ReadDataRandom(new VarTypes[] { VarTypes.Register, VarTypes.Register }, new int[] { 123, 321 });
                //comm.WriteBatch(VarTypes.Register, 200, new short[] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 });
                //comm.ReadDataBatch(VarTypes.Register, 200, 20);
                //comm.ReadDataBatch("d", 101, 1);
            }
            else
            {
                Console.WriteLine("[Błąd] Nie można połączyć się z PLC");
            }
            tcpClient.Close();
            Console.ReadKey();
        }
        
        #region Part of code for generci TcpClinet perform connection for more info please check C# documnetation
        static void ConnectTCP(IPAddress IPAddressToConnect, int portNumber)
        {
            tcpClient.ReceiveTimeout = 5;
            tcpClient.SendTimeout = 5;
            try
            {
                tcpClient = Connect(IPAddressToConnect, portNumber, 1000);
            }
            catch
            {
                Console.WriteLine("Port Open FAIL");
            }
        }
        static TcpClient Connect(IPAddress hostName, int port, int timeout)
        {
            var client = new TcpClient();
            var state = new State { Client = client, Success = true };
            IAsyncResult ar = client.BeginConnect(hostName, port, EndConnect, state);
            state.Success = ar.AsyncWaitHandle.WaitOne(timeout, false);
            if (!state.Success || !client.Connected)
                throw new Exception("Failed to connect.");
            return client;
        }
        private class State
        {
            public TcpClient Client { get; set; }
            public bool Success { get; set; }
        }
        static void EndConnect(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            TcpClient client = state.Client;
            try
            {
                client.EndConnect(ar);
            }
            catch { }
            if (client.Connected && state.Success)
                return;
            client.Close();
        }
        #endregion
    }
}