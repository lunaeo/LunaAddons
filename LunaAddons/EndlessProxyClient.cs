using System;
using System.Linq;
using System.Net.Sockets;

namespace LunaAddons
{
    using EndlessOnline.Communication;

    public class EndlessProxyClient
    {
        public string EOAddress { get; }
        public int EOPort { get; }
        public EndlessProxySession Session { get; }
        public Socket Socket { get; }
        public NetworkStream Stream { get; }
        public ClientPacketProcessor PacketProcessor { get; }
        public byte[] Buffer { get; set; } = new byte[ushort.MaxValue];

        public EndlessProxyClient(EndlessProxySession session, string address, int port)
        {
            this.EOAddress = address;
            this.EOPort = port;

            this.Session = session;
            this.PacketProcessor = new ClientPacketProcessor();

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.Connect(address, port);

            this.Stream = new NetworkStream(this.Socket);
            this.Stream.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.ReceiveCallback), null);
        }

        public void Send(byte[] buffer)
        {
            var decode_buffer = buffer.Skip(2).ToArray();
            this.PacketProcessor.Decode(ref decode_buffer);
            var decode_packet = new Packet(decode_buffer);

            Program.Console.Information("({0}) {1} {2} | (length: {3})", "client->server",
                decode_packet.Family, decode_packet.Action, decode_packet.Length);

            if (this.Socket.Connected)
                this.Socket.Send(buffer);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!this.Socket.Connected)
                {
                    if (this.Stream != null)
                        Program.Console.Error("ProxyClient connection forcibly reset by peer.");
                    return;
                }

                var length = this.Stream.EndRead(ar);
                var received = this.Buffer.Take(length).ToArray();

                if (length == 0)
                {
                    Program.Console.Error("ProxyClient connection forcibly reset by peer. (receiveBytes == 0)");
                    return;
                }

                if (Program.EndlessClient.State != ClientState.Initialized)
                {
                    var packet = new Packet(received.Skip(2).ToArray());

                    if (packet.Family == PacketFamily.Init && packet.Action == PacketAction.Init)
                    {
                        Program.EndlessClient.State = ClientState.Initialized;

                        packet.Skip(3);
                        var recv_multi = packet.GetByte();
                        var send_multi = packet.GetByte();

                        this.PacketProcessor.SetMulti(recv_multi, send_multi);
                    }
                }

                var decode_buffer = received.Skip(2).ToArray();
                this.PacketProcessor.Decode(ref decode_buffer);
                var decode_packet = new Packet(decode_buffer);

                Program.Console.Information("({0}) {1} {2} | (length: {3})", "server->client",
                    decode_packet.Family, decode_packet.Action, decode_packet.Length);

                if (decode_packet.Family == PacketFamily.Talk && decode_packet.Action == PacketAction.Announce)
                {
                    var name = decode_packet.GetBreakString();
                    var message = decode_packet.GetBreakString();

                    if (name == "LunaAddons")
                    {
                        var port = int.Parse(message.Split(' ')[0]);
                        var sessionId = message.Split(' ')[1];

                        Program.EndlessClient.SetupAddonConnection(this.EOAddress, port, sessionId);

                        goto skip_packet;
                    }
                }

                // send the EO client the received bytes from EO server.
                this.Session.Send(received);

                skip_packet:
                this.Stream?.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.ReceiveCallback), null);
            }
            catch (SocketException ex)
            {
                Program.Console.Error("ProxyClient SocketException occured: {0}", ex.ToString());
                return;
            }
        }
    }
}