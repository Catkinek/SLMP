using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PierwszyProgramSLMP
{
    class Communication
    {
        TcpClient client;

        public struct ReadValue
        {
            public bool successRead;
            public short[] value;
        }
        public struct ReadValueBit
        {
            public bool successRead;
            public bool[] value;
        }

        public Communication(TcpClient tcpClient)
        {
            client = tcpClient;
        }
        #region private methods
        private byte ConvertType(VarTypes type)
        {
            byte returnType;
            switch (type)
            {
                case VarTypes.Input: //Fizyczen wejścia sterownika
                    returnType = 0x9c;
                break;
                case VarTypes.Output: //Fizyczne wyjścia sterownika
                    returnType = 0x9d;
                break;
                case VarTypes.Relay: //Zmienne boolowskie sterownika
                    returnType = 0x90;
                break;
                case VarTypes.Alarm: //Alarmy sterownika
                    returnType = 0x93;
                break;
                case VarTypes.Register: //Rejestry 16-bit sterownika
                    returnType = 0xa8;
                break;
                case VarTypes.SpecialRelay: // Bity z góry przypisaną funkcją np. (bit zawsze true, bit pulsujący, statusy PLC)
                    returnType = 0x91;
                break;
                case VarTypes.SpecialRegister: // Rejestry z góry przypisaną funkcją np. (Godzina i data, kody błędów sterownika itd.)
                    returnType = 0xa9;
                break;
                default:
                    returnType = 0xff;
                break;
            }
            return returnType;
        }
        private byte[] ConvertHead(int headAddress)
        {
            byte[] temp = BitConverter.GetBytes(headAddress);
            byte[] empty = { 0x00 };
            byte[] returnHeadAddress = new byte[3];
            Array.Copy(temp, returnHeadAddress, 2);
            Array.Copy(empty, 0, returnHeadAddress, 2, 1);
            return returnHeadAddress;
        }
        private byte[] ConvertNoOfRead(int noOfRead)
        {
            byte[] temp = BitConverter.GetBytes(noOfRead);
            return temp;
        }
        private byte[] ConvertData(VarTypes type, int head, int noOfRead)
        {
            byte[] returnData = new byte[6];
            Array.Copy(ConvertHead(head), returnData, 3);
            returnData[3] = ConvertType(type);
            Array.Copy(ConvertNoOfRead(noOfRead),0, returnData,4, 2);
            return returnData;
        }
        private byte[] ConvertData(VarTypes[] type, int[] head)
        {
            byte[] returnData = new byte[4 * type.Length];
            byte[] temp = new byte[4];
            for (int i = 0; i < type.Length; i++)
            {
                Array.Copy(ConvertHead(head[i]), temp, 3);
                temp[3] = ConvertType(type[i]);
                Array.Copy(temp, 0, returnData, i * 4, 4);
            }
            return returnData;
        }
        #endregion
        public ReadValue ReadDataBatch(VarTypes type, int head, int numberOfRead)
        {
            ReadValue readValue = new ReadValue();
            readValue.successRead = false;
            //0x50 00 - subheader - const
            //0x00 - request destination network const
            //0xff - request destination station const
            //0xff 03 - request destination module i/o const
            //0x00 - request detination multidrop const
            //0x0c 00 - request data length
            //0x10 00 - request timer
            //0x01 04 - command
            //0x00 00 - subsommand
            byte[] frameConst = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
            byte[] requestLength= { 0x0C, 0x00 };
            byte[] requestTimer = { 0x00, 0x00 };
            byte[] requestCommand = { 0x01, 0x04, 0x00, 0x00 };
            byte[] requestData = ConvertData(type,head,numberOfRead);


            byte[] payload = new byte[21];
            Array.Copy(frameConst, 0, payload, 0, 7);
            Array.Copy(requestLength, 0, payload, 7, 2);
            Array.Copy(requestTimer, 0, payload, 9, 2);
            Array.Copy(requestCommand, 0, payload, 11, 4);
            Array.Copy(requestData, 0, payload, 15, 6);
            NetworkStream networkStream = client.GetStream();
            networkStream.Write(payload, 0, payload.Length);
            networkStream.ReadTimeout = 1000;
            byte[] data = new byte[11+numberOfRead*2];
            try
            {
                int numberOfReadBytes = networkStream.Read(data, 0, data.Length);
                if (data[9] == 0 && data[10] == 0)
                {
                    readValue.value = new short[numberOfRead];
                    for (int i = 0; i < numberOfRead; i++)
                    {
                        readValue.value[i] = BitConverter.ToInt16(data, 11+2*i);
                    }
                    readValue.successRead = true;
                    //for (int i = 0; i < numberOfRead; i++)
                    //{
                       // Console.WriteLine("Odczytano poprawnie "+type+(head+i)+": " + readValue.value[i]);
                        //gdy czytamy zmienne bitowe czytanych jest po 16
                        //można zrobić drugą metodę do czytania bitów(pojedyńczych) ale nie wiem czy jest sens
                    //} 
                }
                else
                {
                    short errorCode = BitConverter.ToInt16(data, 9);
                    Console.WriteLine("[Błąd] Nie odczytano poprawnie, kod błędu: " + errorCode.ToString("X"));
                }
            }
            catch
            {
                Console.WriteLine("[Błąd] Utracono odpowiedź ze sterownika");
            }
            return readValue;
        }
        public ReadValueBit ReadDataBatchBit(VarTypes type, int head, int numberOfRead)
        {
            ReadValueBit readValue = new ReadValueBit();
            readValue.successRead = false;
            byte[] frameConst = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
            byte[] requestLength = { 0x0C, 0x00 };
            byte[] requestTimer = { 0x00, 0x00 };
            byte[] requestCommand = { 0x01, 0x04, 0x01, 0x00 };
            byte[] requestData = ConvertData(type, head, numberOfRead);


            byte[] payload = new byte[21];
            Array.Copy(frameConst, 0, payload, 0, 7);
            Array.Copy(requestLength, 0, payload, 7, 2);
            Array.Copy(requestTimer, 0, payload, 9, 2);
            Array.Copy(requestCommand, 0, payload, 11, 4);
            Array.Copy(requestData, 0, payload, 15, 6);
            NetworkStream networkStream = client.GetStream();
            networkStream.Write(payload, 0, payload.Length);
            networkStream.ReadTimeout = 1000;
            byte[] data = new byte[11 + (numberOfRead+1) / 2];
            try
            {
                int numberOfReadBytes = networkStream.Read(data, 0, data.Length);
                if (data[9] == 0 && data[10] == 0)
                {
                    readValue.value = new bool[numberOfRead];
                    for (int i = 0; i < (numberOfRead + 1) / 2; i++)
                    {
                        switch (data[11 + i])
                        {
                            case 0:
                                readValue.value[2*i] = false;
                                if(2*i+1<readValue.value.Length)
                                    readValue.value[2 * i + 1] = false;
                                break;
                            case 1:
                                readValue.value[2 * i] = false;
                                if (2 * i + 1 < readValue.value.Length)
                                    readValue.value[2 * i + 1] = true;
                                break;
                            case 16:
                                readValue.value[2 * i] = true;
                                if (2 * i + 1 < readValue.value.Length)
                                    readValue.value[2 * i + 1] = false;
                                break;
                            case 17:
                                readValue.value[2 * i] = true;
                                if (2 * i + 1 < readValue.value.Length)
                                    readValue.value[2 * i + 1] = true;
                                break;
                        }
                    }
                    readValue.successRead = true;
                }
                else
                {
                    short errorCode = BitConverter.ToInt16(data, 9);
                    Console.WriteLine("[Błąd] Nie odczytano poprawnie, kod błędu: " + errorCode.ToString("X"));
                }
            }
            catch
            {
                Console.WriteLine("[Błąd] Utracono odpowiedź ze sterownika");
            }
            return readValue;
        }
        public ReadValue ReadDataRandom(VarTypes[] type, int[] head)
        {
            ReadValue readValue = new ReadValue();
            readValue.successRead = false;
            byte[] frameConst = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
            byte[] requestLength = BitConverter.GetBytes(type.Length*4+8);
            byte[] requestTimer = { 0x00, 0x00 };
            byte[] requestCommand = { 0x03, 0x04, 0x00, 0x00 };
            byte[] numberOfWords = BitConverter.GetBytes(type.Length);
            byte[] requestData = ConvertData(type, head);

            byte[] payload = new byte[17+type.Length*6];
            Array.Copy(frameConst, 0, payload, 0, 7);
            Array.Copy(requestLength, 0, payload, 7, 2);
            Array.Copy(requestTimer, 0, payload, 9, 2);
            Array.Copy(requestCommand, 0, payload, 11, 4);
            Array.Copy(numberOfWords, 0, payload, 15, 2);
            Array.Copy(requestData, 0, payload, 17, 4*type.Length);
            NetworkStream networkStream = client.GetStream();
            networkStream.Write(payload, 0, payload.Length);
            networkStream.ReadTimeout = 1000;
            byte[] data = new byte[type.Length * 2+11];
            try
            {
                int numberOfReadBytes = networkStream.Read(data, 0, data.Length);
                if (data[9] == 0 && data[10] == 0)
                {
                    readValue.value = new short[type.Length];
                    for (int i = 0; i < type.Length; i++)
                    {
                        readValue.value[i] = BitConverter.ToInt16(data, 11 + 2 * i);
                    }
                    readValue.successRead = true;
                    //for (int i = 0; i < type.Length; i++)
                    //{
                        //Console.WriteLine("Odczytano poprawnie " + type + (head[i] + i) + ": " + readValue.value[i]);
                    //}

                }
                else
                {
                    short errorCode = BitConverter.ToInt16(data, 9);
                    Console.WriteLine("[Błąd] Nie odczytano poprawnie, kod błędu: " + errorCode.ToString("X"));
                }
            }
            catch
            {
                Console.WriteLine("[Błąd] Utracono odpowiedź ze sterownika");
            }
            return readValue;
        }
        public bool WriteBatch(VarTypes type, int head, short[] data)
        {
            bool writeSuccess = false;
            byte[] frameConst = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
            byte[] requestLength = BitConverter.GetBytes(12 + data.Length * 2);
            byte[] requestTimer = { 0x00, 0x00 };
            byte[] requestCommand = { 0x01, 0x14, 0x00, 0x00 };
            byte[] requestData = ConvertData(type, head, data.Length);
            byte[] requestData2 = new byte[data.Length * 2];
            Buffer.BlockCopy(data, 0, requestData2, 0, requestData2.Length);

            byte[] payload = new byte[21+ data.Length * 2];
            Array.Copy(frameConst, 0, payload, 0, 7);
            Array.Copy(requestLength, 0, payload, 7, 2);
            Array.Copy(requestTimer, 0, payload, 9, 2);
            Array.Copy(requestCommand, 0, payload, 11, 4);
            Array.Copy(requestData, 0, payload, 15, 6);
            Array.Copy(requestData2, 0, payload, 21, data.Length * 2);


            NetworkStream networkStream = client.GetStream();
            networkStream.Write(payload, 0, payload.Length);
            networkStream.ReadTimeout = 1000;
            byte[] response = new byte[20];
            try
            {
                int numberOfReadBytes = networkStream.Read(response, 0, response.Length);
                if (response[9] == 0 && response[10] == 0)
                {
                    writeSuccess = true;
                }
                else
                {
                    short errorCode = BitConverter.ToInt16(response, 9);
                    Console.WriteLine("[Błąd] Nie zapisano poprawnie, kod błędu: " + errorCode.ToString("X"));
                }
            }
            catch
            {
                Console.WriteLine("[Błąd] Utracono odpowiedź ze sterownika");
            }
            return writeSuccess;
        }


    }
}
