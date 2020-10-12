using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Binarysharp.MemoryManagement;

namespace LunaAddons
{
    public class Program
    {
        private static EndlessClient EndlessClient { get; set; }
        private static MemorySharp EndlessMemory { get; set; }

        // The version of the launcher to report to the server in the "init" message.
        public static int Version => 1;

        private static int EntryPoint(string args)
        {
            NativeMethods.AllocConsole();

            Console.WriteLine("Runtime Initialized.");
            Console.WriteLine("Arguments: " + args);

            EndlessMemory = new MemorySharp(Process.GetCurrentProcess());
            EndlessClient = new EndlessClient(EndlessMemory);

            while (true)
                Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            var inject_filename = Path.Combine("luna", "inject.exe");
            var startup_filename = Process.GetCurrentProcess().MainModule.FileName;

            var endless_process = Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = "endless.exe"
            });

            var self_inject_arguments =
                string.Format("-m {0} -i \"{1}\" -l {2} -a \"{3}\" -n {4}", "EntryPoint", startup_filename, "LunaAddons.Program", "", endless_process.Id);

            // self inject process
            Process.Start(new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = inject_filename,
                Arguments = self_inject_arguments
            });
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool FreeConsole();
    }
}