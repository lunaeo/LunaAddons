using System;
using System.Linq;
using System.Threading;
using NetCoreServer;

namespace LunaAddons
{
    public class EndlessProxySession : TcpSession
    {
        public string EOServerHost { get; }
        public int EOServerPort { get; }
        internal EndlessProxyServer EndlessProxyServer { get; set; }
        private EndlessProxyClient EndlessProxyClient { get; set; }

        public EndlessProxySession(TcpServer server, string host, int port) : base(server)
        {
            try
            {
                this.EndlessProxyServer = (EndlessProxyServer)server;
                this.EOServerHost = host;
                this.EOServerPort = port;
                this.EndlessProxyClient = new EndlessProxyClient(this, "127.0.0.1", 8000);
            }
            catch (Exception exception)
            {
                Program.Console.Error("An exception occured in ProxySession: {message}", exception.Message);
                this.Server.DisconnectAll();
            }
        }

        protected override void OnDisconnected()
        {
            this.EndlessProxyClient?.Socket?.Disconnect(true);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (this.EndlessProxyClient == null)
                return;

            while (!this.EndlessProxyClient.Socket.Connected)
                Thread.Sleep(1);

            this.EndlessProxyClient.Send(buffer.Skip((int)offset).Take((int)size).ToArray());
        }
    }
}