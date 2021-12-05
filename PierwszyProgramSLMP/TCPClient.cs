using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PierwszyProgramSLMP
{
    class TCPClient
    {
        TcpClient tcpClient = new TcpClient();
        IPAddress ip;

        public TCPClient(byte[] ipAdr)
        {
            ip = new IPAddress(ipAdr);
        }

        void ConnectTCP(IPAddress IPAddressToConnect, int portNumber)
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
    }
}
