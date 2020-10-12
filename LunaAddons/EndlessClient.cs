using System;
using System.Linq;
using Binarysharp.MemoryManagement;

using DetourPacket = LunaAddons.Detours.Packet;
using EndlessPacket = EndlessOnline.Communication.Packet;

namespace LunaAddons
{
    using EndlessOnline.Communication;
    using Detours;

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

        public EndlessClient(MemorySharp memory)
        {
            this.Memory = memory;
            this.State = ClientState.Uninitialized;
            this.Map = new Map(this);

            this.PacketProcessor = new ClientPacketProcessor();
            this.AddonConnection = new AddonConnection(this);

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

                        this.PacketProcessor.SetMulti(decode.GetByte(), decode.GetByte());
                        this.SocketId = packet.Socket;
                    }

                    return new InterceptResponse(false);
                }

                var buffer = packet.Buffer.Skip(2).ToArray();
                this.PacketProcessor.Decode(ref buffer);
                var decoded = new EndlessPacket(buffer);

                Console.WriteLine("({0}) {1} {2} | (length: {3})", packet.Channel == PacketChannel.Send ? "client-server" : "server->client",
                    decoded.Family, decoded.Action, decoded.Length);

                if (decoded.Family == PacketFamily.Welcome && decoded.Action == PacketAction.Message)
                    this.AddonConnection.Send(new AddonMessage("init", Program.Version));

                if (decoded.Family == PacketFamily.AutoRefresh && decoded.Action == PacketAction.Init)
                {
                    var decoded_buffer = decoded.Get().Skip(2).ToArray();
                    this.AddonConnection.MessageDeserializer.AddBytes(decoded_buffer);
                }

                return new InterceptResponse(false);
            }));
        }
    }
}