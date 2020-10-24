namespace LunaAddons
{
    public class Map
    {
        public EndlessClient Client { get; }

        public Map(EndlessClient client)
        {
            this.Client = client;
        }

        /// <summary>
        /// If outside the map, the value will be >254
        /// </summary>
        internal ushort GetHoverTileX()
        {
            var base_address_offset = 0x00275688;
            var offsets = new[] { 0x04, 0x36C, 0x14FC20 };

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        /// <summary>
        /// If outside the map, the value will be >254
        /// </summary>
        /// <returns></returns>
        internal ushort GetHoverTileY()
        {
            var base_address_offset = 0x00275688;
            var offsets = new[] { 0x04, 0x36C, 0x14FC24 };

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal ushort GetOverlayA(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x3D090 + 0x3D090 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetOverlayA(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x3D090 + 0x3D090 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetTop(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x3D090 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetTop(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x3D090 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetRightWall(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x1E848 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetRightWall(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x1E848 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetShadow(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetShadow(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetOverlayB(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetOverlayB(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetSpecial(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetSpecial(byte x, byte y, bool is_wall)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 + 0x1E848 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, is_wall ? (ushort)1 : (ushort)0);
        }

        internal ushort GetDownWall(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x3D090 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetDownWall(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x3D090 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetOverlay(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x7A120 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetOverlay(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 + 0x7A120 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetGround(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x7A120 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetGround(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 - 0x7A120 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }

        internal ushort GetObject(byte x, byte y)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 };

            offsets[3] += x > 0 ? (500 * x) : 0;
            offsets[3] += y * 2;

            return this.Client.Memory.GetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets);
        }

        internal void SetObject(byte x, byte y, ushort id)
        {
            var base_address_offset = 0x00305400;
            var offsets = new[] { 0x60, 0x2C, 0x36C, 0x7A140 };

            offsets[3] += x > 0 ? 500 * x : 0;
            offsets[3] += y * 2;

            this.Client.Memory.SetPointerValue<ushort>(this.Client.Memory.Modules.MainModule.BaseAddress + base_address_offset, offsets, id);
        }
    }
}