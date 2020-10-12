using System;
using System.Linq;

namespace LunaAddons
{
    using EndlessOnline.Communication;

    /// <summary> Used to add a message handler to the OnMessage event of an instance of AddonConnection. </summary>
    public delegate void MessageReceivedEventHandler(object sender, AddonMessage e);

    public class AddonConnection
    {
        /// <summary>
        /// A property used to add a message handler to the OnMessage event of an instance of Connection.
        /// </summary>
        public event MessageReceivedEventHandler OnMessage;

        internal AddonConnection(EndlessClient client)
        {
            this.Client = client;
            this.MessageDeserializer = new BinaryDeserializer();

            this.MessageDeserializer.OnDeserializedMessage += (e) =>
            {
                this.OnMessage?.Invoke(this, e);
            };
        }

        public void Send(AddonMessage message)
        {
            var serialized = new BinarySerializer().Serialize(message);
            var chunks = serialized.AsChunks(2048);

            foreach (var chunk in chunks)
            {
                var reply = new Packet(PacketFamily.AutoRefresh, PacketAction.Init);
                reply.AddBytes(chunk);
                this.Send(reply);
            }
        }

        internal void Send(Packet packet)
        {
            try
            {
                var data = packet.Get();
                var length = Packet.EncodeNumber(data.Length, 2);
                this.Client.PacketProcessor.Encode(ref data);

                this.Client.NetworkDetour.Send(this.Client.SocketId, Packet.EncodeNumber(data.Length, 2).Concat(data).ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal EndlessClient Client { get; }
        internal readonly BinaryDeserializer MessageDeserializer;
    }
}