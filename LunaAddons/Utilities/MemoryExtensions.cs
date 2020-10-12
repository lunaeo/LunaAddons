using System;
using Binarysharp.MemoryManagement;

namespace LunaAddons
{
    public static class MemoryExtensions
    {
        public static T GetPointerValue<T>(this MemorySharp ms, IntPtr base_address, int[] offsets)
        {
            var ptr = IntPtr.Add((IntPtr)ms[base_address, false].Read<int>(), offsets[0]);

            for (var i = 1; i < offsets.Length; i++)
                ptr = IntPtr.Add((IntPtr)ms[ptr, false].Read<int>(), offsets[i]);

            return ms[ptr, false].Read<T>();
        }

        public static void SetPointerValue<T>(this MemorySharp ms, IntPtr base_address, int[] offsets, T value)
        {
            var ptr = IntPtr.Add((IntPtr)ms[base_address, false].Read<int>(), offsets[0]);

            for (var i = 1; i < offsets.Length; i++)
                ptr = IntPtr.Add((IntPtr)ms[ptr, false].Read<int>(), offsets[i]);

            ms[ptr, false].Write<T>(value);
        }
    }
}