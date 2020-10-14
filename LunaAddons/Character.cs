namespace LunaAddons
{
    public class Character
    {
        public EndlessClient Client { get; }

        public Character(EndlessClient client)
        {
            this.Client = client;
        }

        /// <summary>
        /// Get the sitting value of the character.
        /// </summary>
        /// <returns> If sitting, returns true. Otherwise, false. </returns>
        public bool Get()
        {
            var base_address_offset = 0x001A766C;
            var offsets = new[] { 0x20, 0x390, 0xC, 0xD8 };
            var value = this.Client.Memory.GetPointerValue<byte>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);

            if (value == 1)
                return true;
            return false;
        }

        public void Sit()
        {
            var base_address_offset = 0x001A766C;
            var offsets = new[] { 0x20, 0x390, 0xC, 0xD8 };
            this.Client.Memory.SetPointerValue<byte>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, 1);
        }

        public void Stand()
        {
            var base_address_offset = 0x001A766C;
            var offsets = new[] { 0x20, 0x390, 0xC, 0xD8 };
            this.Client.Memory.SetPointerValue<byte>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, 0);
        }
    }
}