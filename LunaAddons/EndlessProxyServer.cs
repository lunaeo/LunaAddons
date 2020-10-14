using System;
using System.Net;
using System.Net.Sockets;

namespace LunaAddons
{
    using NetCoreServer;

    public class EndlessProxyServer : TcpServer
    {
        public EndlessProxyServer(IPAddress address, int port) : base(address, port)
        {
        }

        protected override TcpSession CreateSession()
        {
            return new EndlessProxySession(this, "127.0.0.1", 8000);
        }

        protected override void OnError(SocketError error)
        {
            Program.Console.Error($"Proxy TCP server caught an error with code {error}", error);
        }
    }
}