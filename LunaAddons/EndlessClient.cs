using System;
using System.Linq;
using Binarysharp.MemoryManagement;

using DetourPacket = LunaAddons.Detours.Packet;
using EndlessPacket = EndlessOnline.Communication.Packet;

namespace LunaAddons
{
    using EndlessOnline.Communication;
    using Detours;
    using NetCoreServer;
    using System.Net;

    /// <summary>
    /// Used to add a message handler to the OnMessage event of an instance of AddonConnection.
    /// </summary>
    public delegate void MessageReceivedEventHandler(object sender, AddonMessage e);

    public class AddonConnection : TcpClient
    {
        internal EndlessClient Client { get; }
        internal BinarySerializer Serializer { get; set; }
        internal BinaryDeserializer Deserializer { get; set; }
        public string SessionId { get; }

        /// <summary>
        /// A property used to add a message handler to the OnMessage event of an instance of Connection.
        /// </summary>
        public event MessageReceivedEventHandler OnMessage;

        public AddonConnection(string address, int port, string sessionId, EndlessClient client) : base(address, port)
        {
            this.Client = client;
            this.Serializer = new BinarySerializer();
            this.Deserializer = new BinaryDeserializer();
            this.SessionId = sessionId;

            this.Deserializer.OnDeserializedMessage += (e) =>
            {
                this.OnMessage?.Invoke(this, e);
            };
        }

        protected override void OnConnected()
        {
            this.Send("init", this.Client.AddonProtocolVersion, this.SessionId);
        }

        public void Send(string type, params object[] parameters) =>
            this.Send(new AddonMessage(type, parameters));

        public void Send(AddonMessage message) => 
            this.Send(this.Serializer.Serialize(message));

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            this.Deserializer.AddBytes(buffer.Skip((int)offset).Take((int)size).ToArray());
        }
    }

    public class EndlessClient
    {
        internal MemorySharp Memory { get; set; }
        internal ClientState State { get; set; }
        internal Map Map { get; set; }

        // network detours (required for addon<->server communication)
        internal NetworkDetour NetworkDetour { get; set; }
        internal PacketProcessor PacketProcessor { get; set; }
        internal AddonConnection AddonConnection { get; set; }
        internal int SocketId { get; private set; }
        internal int AddonProtocolVersion => 1;

        public EndlessClient(MemorySharp memory)
        {
            this.Memory = memory;
            this.State = ClientState.Uninitialized;
            this.Map = new Map(this);

            this.PacketProcessor = new ClientPacketProcessor();

            this.AddonConnection.OnMessage += (s, e) =>
            {
                Console.WriteLine(e);

                switch (e.Type)
                {
                    case "mutate":
                        var type = e.GetInt(0);
                        var x = e.GetInt(1);
                        var y = e.GetInt(2);
                        var id = e.GetInt(3);

                        if (Enum.IsDefined(typeof(MutateType), type))
                        {
                            var mutation_type = (MutateType)type;

                            switch (mutation_type)
                            {
                                case MutateType.Ground:
                                    this.Map.SetGround((byte)x, (byte)y, (ushort)id);
                                    break;

                                case MutateType.Object:
                                    this.Map.SetObject((byte)x, (byte)y, (ushort)id);
                                    break;
                            }
                        }
                        break;
                }
            };

            this.SetupNetworkDetour();
        }

        private void SetupNetworkDetour()
        {
            this.NetworkDetour = new NetworkDetour().Install(PacketChannel.Send | PacketChannel.WSARecv,
                new InterceptCallback((DetourPacket packet) =>
            {
                if (this.State != ClientState.Initialized)
                {
                    if (packet.Channel == PacketChannel.WSARecv)
                    {
                        var decode = new EndlessPacket(packet.Buffer.Skip(2).ToArray());

                        if (decode.Family != PacketFamily.Init || decode.Action != PacketAction.Init)
                            return new InterceptResponse(false);

                        this.State = ClientState.Initialized;
                        decode.Skip(3);

                        var recv_multi = decode.GetByte();
                        var send_multi = decode.GetByte();

                        Console.WriteLine("recv_multi: " + recv_multi);
                        Console.WriteLine("send_multi: " + send_multi);

                        this.PacketProcessor.SetMulti(recv_multi, send_multi);
                        this.SocketId = packet.Socket;
                    }

                    return new InterceptResponse(false);
                }

                var buffer = packet.Buffer.Skip(2).ToArray();
                this.PacketProcessor.Decode(ref buffer);
                var decoded = new EndlessPacket(buffer);

                Console.WriteLine("({0}) {1} {2} | (length: {3})", packet.Channel == PacketChannel.Send ? "client-server" : "server->client",
                    decoded.Family, decoded.Action, decoded.Length);

                if (decoded.Family == PacketFamily.Init && decoded.Action == PacketAction.Open)
                {
                    var port = decoded.GetInt();
                    var sessionId = decoded.GetEndString();

                    this.AddonConnection = new AddonConnection(packet.Source.Address.ToString(), port, sessionId, this);
                }

                return new InterceptResponse(false);
            }));
        }
    }
}