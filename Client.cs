using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace BotWaRz
{
    class Client : TcpClient, IDisposable
    {
        private static string hostname = "botwarz.eset.com";
        private static int port = 8080; //2000;
        private static int bufferSize = 8192;

        private NetworkStream stream;
        private StreamWriter writer;
        private byte[] buffer = new byte[bufferSize];


        // --------------------------------------------------------------------
        public Client()
            : base(hostname, port)
        {
            // Disable the Nagle Algorithm for this tcp socket.
            this.NoDelay = true;
            this.Client.NoDelay = true;

            stream = this.GetStream();
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;
        }


        // --------------------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            writer.Close();
            stream.Close();

            base.Dispose(disposing);
        }


        // --------------------------------------------------------------------
        public string ReadString(int timeout = 0)
        {
            try
            {
                if (timeout > 0)
                    ReceiveTimeout = timeout;
                int bytesReturned = stream.Read(buffer, 0, buffer.Length);
                ReceiveTimeout = 0;
                return Encoding.ASCII.GetString(buffer, 0, bytesReturned);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }


        // --------------------------------------------------------------------
        public void Send(object value)
        {
            //byte[] buffer = Encoding.ASCII.GetBytes(Serialize(value));
            //stream.Write(buffer, 0, buffer.Length);
            //stream.Flush();

            try
            {
                writer.Write(Serialize(value));
            }
            catch (Exception)
            { }
        }


        // --------------------------------------------------------------------
        private static string Serialize(object value)
        {
            Console.WriteLine(JsonConvert.SerializeObject(value, Formatting.Indented));

            string json = JsonConvert.SerializeObject(value);
            return json += "\n";
        }
    }
}
