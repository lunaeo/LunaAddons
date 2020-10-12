using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace LunaAddons
{
    /// <summary>
    /// A class for assisting with installing and managing detours.
    /// </summary>
    public class Detour
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleA", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, int flNewProtect, ref int lpflOldProtect);

        [DllImport("kernel32.dll", EntryPoint = "lstrcpynA", CharSet = CharSet.Ansi)]
        private static extern IntPtr lstrcpyn(byte[] lpString1, byte[] lpString2, int iMaxLength);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private const int PAGE_EXECUTE_READWRITE = 0x40;
        private IntPtr ProcAddress;
        private int lpflOldProtect = 0;

        private byte[] oldEntry = new byte[5];
        private byte[] newEntry = new byte[5];
        private IntPtr OldAddress;

        /// <summary>
        /// Indicates whether the detour has been successfully installed.
        /// </summary>
        public bool SuccessfullyInstalled = false;

        /// <summary>
        /// Install the specified detour in the running process.
        /// </summary>
        /// <param name="moduleName"> The name of the module to hook. </param>
        /// <param name="procName"> The name of the procedure to hook. </param>
        /// <param name="callback"> The callback for the hook. </param>
        /// <returns></returns>
        public Detour Install(string moduleName, string procName, Delegate callback)
        {
            var lpAddress = Marshal.GetFunctionPointerForDelegate(callback);

            var hModule = GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero)
                throw new Exception("Unable to install detour. The module name specified does not exist.");

            this.ProcAddress = GetProcAddress(hModule, procName);
            if (ProcAddress == IntPtr.Zero)
                throw new Exception("Unable to install detour. The procedure name specified does not exist.");

            if (!VirtualProtect(ProcAddress, 5, PAGE_EXECUTE_READWRITE, ref lpflOldProtect))
                throw new Exception("Unable to install detour. The virtual protection was unable to be modified.");

            Marshal.Copy(ProcAddress, oldEntry, 0, 5);
            newEntry = AddBytes(new byte[1] { 233 }, BitConverter.GetBytes((int)lpAddress - (int)ProcAddress - 5));
            Marshal.Copy(newEntry, 0, ProcAddress, 5);
            oldEntry = AddBytes(oldEntry, new byte[5] { 233, 0, 0, 0, 0 });
            OldAddress = lstrcpyn(oldEntry, oldEntry, 0);
            Marshal.Copy(BitConverter.GetBytes((double)((int)ProcAddress - (int)OldAddress - 5)), 0, (IntPtr)(OldAddress.ToInt32() + 6), 4);
            FreeLibrary(hModule);

            this.SuccessfullyInstalled = true;
            return this;
        }

        /// <summary>
        /// Suspend the detour by restoring the JMP instruction to the original address.
        /// </summary>
        public void Suspend() => Marshal.Copy(oldEntry, 0, ProcAddress, 5);

        /// <summary>
        /// Continue the detour by changing the JMP instruction to the new address.
        /// </summary>
        public void Continue() => Marshal.Copy(newEntry, 0, ProcAddress, 5);

        /// <summary>
        /// Uninstall the detour by restoring the the JMP instruction to the original address.
        /// </summary>
        /// <returns></returns>
        public bool Uninstall()
        {
            if (ProcAddress == IntPtr.Zero)
                return false;

            Marshal.Copy(oldEntry, 0, ProcAddress, 5);
            ProcAddress = IntPtr.Zero;
            return true;
        }

        private byte[] AddBytes(byte[] a, byte[] b)
        {
            var arrayList = new ArrayList();

            for (var i = 0; i < a.Length; i++) arrayList.Add(a[i]);
            for (var i = 0; i < b.Length; i++) arrayList.Add(b[i]);

            return (byte[])arrayList.ToArray(typeof(byte));
        }
    }
}