using System;
using System.Threading;
using Binarysharp.MemoryManagement;
using Binarysharp.MemoryManagement.Native;

namespace LunaAddons
{
    public class EndlessClient
    {
        internal MemorySharp Memory { get; }
        internal Character Character { get; }
        internal Map Map { get; }
        internal AddonConnection AddonConnection { get; private set; }

        public EndlessClient(MemorySharp memory)
        {
            this.Memory = memory;
            this.Character = new Character(this);
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
                    case "cursor":
                        var tile_x = (int)this.Map.GetHoverTileX();
                        var tile_y = (int)this.Map.GetHoverTileY();

                        this.AddonConnection.Send("cursor", tile_x, tile_y);
                        break;

                    case "sit":
                        this.Character.Sit();
                        break;

                    case "stand":
                        this.Character.Stand();
                        break;

                    case "mutate":
                    {
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
                }
            };
        }
    }
}