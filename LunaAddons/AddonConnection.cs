using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace LunaAddons
{
    /// <summary>
    /// Used to add a message handler to the OnMessage event of an instance of AddonConnection.
    /// </summary>
    public delegate void MessageReceivedEventHandler(object sender, AddonMessage e);

    public class AddonConnection
    {
        internal EndlessClient Client { get; }
        internal BinarySerializer Serializer { get; set; }
        internal BinaryDeserializer Deserializer { get; set; }
        public string SessionId { get; }
        public Socket Socket { get; }
        public NetworkStream Stream { get; }
        public byte[] Buffer = new byte[ushort.MaxValue];

        /// <summary>
        /// A property used to add a message handler to the OnMessage event of an instance of Connection.
        /// </summary>
        public event MessageReceivedEventHandler OnMessage;

        public AddonConnection(string address, int port, string sessionId, EndlessClient client)
        {
            this.Client = client;
            this.Serializer = new BinarySerializer();
            this.Deserializer = new BinaryDeserializer();
            this.SessionId = sessionId;

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.Connect(address, port);

            this.Stream = new NetworkStream(this.Socket);
            this.Stream.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.ReceiveCallback), null);

            this.Deserializer.OnDeserializedMessage += (e) =>
            {
                this.OnMessage?.Invoke(this, e);
            };

            this.Send("init", Program.AddonVersion, this.SessionId);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!this.Socket.Connected)
                {
                    if (this.Stream != null)
                        Program.Console.Error("ProxyClient connection forcibly reset by peer.");

                    this.Client.AddonConnection.Socket?.Disconnect(true);
                    return;
                }

                var length = this.Stream.EndRead(ar);
                var received = this.Buffer.Take(length).ToArray();

                if (length == 0)
                {
                    Program.Console.Error("ProxyClient connection forcibly reset by peer. (receivedBytes == 0)");
                    this.Client.AddonConnection.Socket?.Disconnect(true);
                    return;
                }

                this.Deserializer.AddBytes(received);
                this.Stream?.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.ReceiveCallback), null);
            }
            catch (IOException)
            {
                if (this.Socket != null && this.Socket.Connected)
                    this.Socket.Close();

                if (this.Client.AddonConnection.Socket != null && this.Client.AddonConnection.Socket.Connected)
                    this.Client.AddonConnection.Socket.Disconnect(true);
            }
            catch (SocketException)
            {
                if (this.Socket != null && this.Socket.Connected)
                    this.Socket.Close();

                if (this.Client.AddonConnection.Socket != null && this.Client.AddonConnection.Socket.Connected)
                    this.Client.AddonConnection.Socket.Disconnect(true);
            }
        }

        public void Send(string type, params object[] parameters) =>
            this.Send(new AddonMessage(type, parameters));

        public void Send(AddonMessage message) =>
            this.Socket.Send(this.Serializer.Serialize(message));
    }
}