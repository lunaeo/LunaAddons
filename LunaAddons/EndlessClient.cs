using System;
using Binarysharp.MemoryManagement;

namespace LunaAddons
{
    public class EndlessClient
    {
        internal MemorySharp Memory { get; set; }
        internal ClientState State { get; set; }
        internal Map Map { get; set; }
        internal AddonConnection AddonConnection { get; set; }

        public EndlessClient(MemorySharp memory)
        {
            this.Memory = memory;
            this.State = ClientState.Uninitialized;
            this.Map = new Map(this);
        }

        public void SetupAddonConnection(string host, int port, string sessionId)
        {
            this.AddonConnection = new AddonConnection(host, port, sessionId, this);
            this.AddonConnection.OnMessage += (s, e) =>
            {
                Program.Console.Information("An addon message was received from the server:\n" + e);

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
        }
    }
}