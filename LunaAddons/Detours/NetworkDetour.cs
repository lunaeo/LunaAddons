using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace LunaAddons.Detours
{
    /// <summary>
    /// A class for extending the functionality of the built-in networking detour.
    /// </summary>
    public static class NetworkDetourExtensions
    {
        /// <summary>
        /// Retrieve the host name entry from DNS for the specified endpoint.
        /// </summary>
        /// <returns> The host name entry from DNS for the specified endpoint. </returns>
        public static string GetHostName(this IPEndPoint endpoint) => Dns.GetHostEntry(endpoint.Address.ToString()).HostName;

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int memcmp(byte[] b1, byte[] b2, long count);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [StructLayout(LayoutKind.Sequential)]
        internal struct sockaddr
        {
            public short sin_family;
            public ushort sin_port;
            public in_addr sin_addr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] sin_zero;
        }

        internal struct in_addr
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] sin_addr;
        }

        [DllImport("ws2_32.dll")]
        internal static extern int getpeername(IntPtr socketHandle, ref sockaddr socketAddress, ref int socketAddressSize);

        [DllImport("ws2_32.dll", SetLastError = true)]
        internal static extern int getsockname(IntPtr socketHandle, ref sockaddr socketAddress, ref int socketAddressSize);

        internal static IPEndPoint GetSourceIPEndPoint(this IntPtr socket)
        {
            var address = new sockaddr();
            var nameLength = Marshal.SizeOf(address);
            var result = getsockname(socket, ref address, ref nameLength);

            var octets = new byte[4];

            for (var i = 0; i < 4; i++)
                octets[i] = address.sin_addr.sin_addr[i];

            return new IPEndPoint(IPAddress.Parse(string.Join(".", octets)), address.sin_port);
        }

        internal static IPEndPoint GetDestinationIPEndPoint(this IntPtr socket)
        {
            var address = new sockaddr();
            var nameLength = 0x10;
            var result = getpeername(socket, ref address, ref nameLength);

            var octets = new byte[4];

            for (var i = 0; i < 4; i++)
                octets[i] = address.sin_addr.sin_addr[i];

            return new IPEndPoint(IPAddress.Parse(string.Join(".", octets)), address.sin_port);
        }

        internal static byte[] Copy(this IntPtr buffer, int index, int length)
        {
            var _buffer = new byte[length];
            Marshal.Copy(buffer, _buffer, index, length);

            return _buffer;
        }

        internal static bool FastSequenceEquals(this byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
    }

    /// <summary>
    /// The response indicating whether to filter the packet and, if so, with the buffer provided.
    /// </summary>
    public class InterceptResponse
    {
        /// <summary>
        /// Boolean indicating whether the packet will be filtered.
        /// </summary>
        public bool Filter { get; }

        /// <summary>
        /// The content of the filtered packet.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// The response indicating whether to filter the packet and, if so, with the buffer provided.
        /// </summary>
        /// <param name="filter"> Boolean indicating whether the packet will be filtered. </param>
        /// <param name="buffer"> The new content of the packet, if the packet is being filtered. </param>
        public InterceptResponse(bool filter, byte[] buffer = null)
        {
            if (filter && (buffer == null || buffer.Length == 0))
                throw new Exception("The buffer must be non-empty if the response is set to filter.");

            this.Filter = filter;
            this.Buffer = buffer;
        }
    }

    /// <summary>
    /// <para>
    /// When a packet is sent or received, this callback will be executed containing the packet.
    /// </para>
    /// <para>
    /// <see cref="InterceptResponse"/> is expected to be returned specifying whether or not the
    /// packet is to be filtered, and if so, the buffer to replace the packet contents with.
    /// </para>
    /// </summary>
    /// <param name="packet"> The packet being sent or received. </param>
    public delegate InterceptResponse InterceptCallback(Packet packet);

    /// <summary>
    /// The packet being sent or received from a channel.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// The channel in which the packet was sent or received.
        /// </summary>
        public PacketChannel Channel { get; internal set; }

        /// <summary>
        /// The IPEndPoint the packet originated from.
        /// </summary>
        public IPEndPoint Source { get; internal set; }

        /// <summary>
        /// The IPEndPoint the packet is being sent to.
        /// </summary>
        public IPEndPoint Destination { get; internal set; }

        /// <summary>
        ///  The content of the packet.
        /// </summary>
        public byte[] Buffer { get; internal set; }

        /// <summary>
        /// The length of the buffer.
        /// (note: not the length of the packet) <see cref="Buffer"/>
        /// </summary>
        public int Length { get; internal set; }

        /// <summary>
        /// The ID of the socket.
        /// </summary>
        public int Socket { get; internal set; }
    }

    /// <summary>
    /// The currently supported WS2_32 derived methods
    /// </summary>
    [Flags]
    public enum PacketChannel
    {
        Send = 1,
        Recv = 2,
        SendTo = 4,
        RecvFrom = 8,
        WSASend = 16,
        WSARecv = 32,

        All = Send | Recv | SendTo | RecvFrom | WSASend | WSARecv
    }

    /// <summary>
    /// A built-in detour handler for WINSOCK networking.
    /// </summary>
    public class NetworkDetour : IDisposable
    {
        public static Detour SendHook = new Detour();
        public static Detour RecvHook = new Detour();

        public static Detour SendToHook = new Detour();
        public static Detour RecvFromHook = new Detour();

        public static Detour WSASendHook = new Detour();
        public static Detour WSARecvHook = new Detour();

        internal PacketChannel SelectedChannels = PacketChannel.All;
        internal static InterceptCallback OnReceivePacket { get; set; }

        /// <summary>
        /// NOTE: You can currently only filter the packets for SEND and RECV channels.
        /// </summary>
        /// <param name="channels"> By default, all channels are selected. </param>
        /// <param name="interceptCallback"> The packets intercepted will be received here for logging, and filtering if necessary. </param>
        public NetworkDetour Install(PacketChannel channels, InterceptCallback interceptCallback)
        {
            SelectedChannels = channels;
            OnReceivePacket = interceptCallback;

            if (SelectedChannels.HasFlag(PacketChannel.Send))
                SendHook = new Detour().Install("ws2_32.dll", "send", callback_repl_send);

            if (SelectedChannels.HasFlag(PacketChannel.Recv))
                RecvHook = new Detour().Install("ws2_32.dll", "recv", callback_repl_recv);

            if (SelectedChannels.HasFlag(PacketChannel.SendTo))
                SendToHook = new Detour().Install("ws2_32.dll", "sendto", callback_repl_send_to);

            if (SelectedChannels.HasFlag(PacketChannel.RecvFrom))
                RecvFromHook = new Detour().Install("ws2_32.dll", "recvfrom", callback_repl_recv_from);

            if (SelectedChannels.HasFlag(PacketChannel.WSASend))
                WSASendHook = new Detour().Install("ws2_32.dll", "WSASend", callback_repl_wsa_send);

            if (SelectedChannels.HasFlag(PacketChannel.WSARecv))
                WSARecvHook = new Detour().Install("ws2_32.dll", "WSARecv", callback_repl_wsa_recv);

            return this;
        }

        /// <summary>
        /// Uninstall the network detours.
        /// </summary>
        public void Dispose()
        {
            OnReceivePacket = null;

            if (SendHook.SuccessfullyInstalled)
                SendHook.Uninstall();

            if (RecvHook.SuccessfullyInstalled)
                RecvHook.Uninstall();

            if (SendToHook.SuccessfullyInstalled)
                SendToHook.Uninstall();

            if (RecvFromHook.SuccessfullyInstalled)
                RecvFromHook.Uninstall();

            if (WSASendHook.SuccessfullyInstalled)
                WSASendHook.Uninstall();

            if (WSARecvHook.SuccessfullyInstalled)
                WSARecvHook.Uninstall();
        }

        /// <summary>
        /// A method to call WS2_32 send() with the packet specified.
        /// </summary>
        /// <param name="socket"> Socket ID </param>
        /// <param name="buffer"> Packet content </param>
        public void Send(int socket, byte[] buffer)
        {
            var pinned_array = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var modified_pointer = pinned_array.AddrOfPinnedObject();

            var length = send((IntPtr)socket, modified_pointer, buffer.Length, 0);
            pinned_array.Free();
        }

        internal static int repl_send(IntPtr socket, IntPtr pBuffer, int len, int pFlags)
        {
            SendHook.Suspend();

            var buffer = pBuffer.Copy(0, len);
            var length = -1;

            var packet = new Packet()
            {
                Channel = PacketChannel.Send,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = len,
                Socket = (int)socket
            };

            try
            {
                var response = OnReceivePacket(packet);

                if (!response.Filter)
                    goto finish;

                if (response.Buffer.FastSequenceEquals(buffer))
                    goto finish;

                var pinned_array = GCHandle.Alloc(response.Buffer, GCHandleType.Pinned);
                var modified_pointer = pinned_array.AddrOfPinnedObject();

                length = send(socket, modified_pointer, response.Buffer.Length, pFlags);
                pinned_array.Free();
                return length;
            }
            catch { } // TODO: whenever an exception occurs, log the exception for the plugin.

            finish:
            length = send(socket, pBuffer, len, pFlags);
            SendHook.Continue();
            return length;
        }

        internal static int repl_recv(IntPtr socket, IntPtr pBuffer, int len, int pFlags)
        {
            RecvHook.Suspend();

            var length = recv(socket, pBuffer, len, pFlags);

            if (length <= 0)
            {
                RecvHook.Continue();
                return length;
            }

            var buffer = pBuffer.Copy(0, length);

            var packet = new Packet()
            {
                Channel = PacketChannel.Recv,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = length,
                Socket = (int)socket
            };

            try
            {
                var response = OnReceivePacket(packet);

                if (!response.Filter)
                    goto finish;

                if (response.Buffer.FastSequenceEquals(buffer))
                    goto finish;

                NetworkDetourExtensions.WriteProcessMemory(Process.GetCurrentProcess().Handle, pBuffer, response.Buffer, (uint)response.Buffer.Length, out var bytesWritten);

                RecvHook.Continue();
                return response.Buffer.Length;
            }
            catch { } // TODO: whenever an exception occurs, log the exception for the plugin.

            finish:
            RecvHook.Continue();
            return length;
        }

        internal static int repl_recv_from(IntPtr socket, IntPtr pBuffer, int len, int socketFlags, ref byte[] socketAddress, ref int socketAddressSize)
        {
            RecvFromHook.Suspend();

            var length = recvfrom(socket, pBuffer, len, socketFlags, ref socketAddress, ref socketAddressSize);
            var buffer = pBuffer.Copy(0, length);

            var packet = new Packet()
            {
                Channel = PacketChannel.SendTo,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = length,
                Socket = (int)socket
            };

            // TODO: allow modifying recv_from packets
            try { OnReceivePacket(packet); } catch { }

            RecvFromHook.Continue();

            return length;
        }

        internal static int repl_send_to(IntPtr socket, IntPtr pBuffer, int len, int socketFlags, byte[] socketAddress, int socketAddressSize)
        {
            SendToHook.Suspend();

            var length = sendto(socket, pBuffer, len, socketFlags, socketAddress, socketAddressSize);
            var buffer = pBuffer.Copy(0, length);

            var packet = new Packet()
            {
                Channel = PacketChannel.SendTo,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = length,
                Socket = (int)socket
            };

            // TODO: allow modifying send_to packets
            try { OnReceivePacket(packet); } catch { }

            SendToHook.Continue();

            return length;
        }

        internal static int repl_wsa_send(IntPtr socket, ref WSABuffer wsaBuffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine)
        {
            WSASendHook.Suspend();

            var length = WSASend(socket, ref wsaBuffer, bufferCount, out bytesTransferred, ref socketFlags, overlapped, completionRoutine);
            var buffer = wsaBuffer.Pointer.Copy(0, wsaBuffer.Length);

            var packet = new Packet()
            {
                Channel = PacketChannel.WSASend,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = buffer.Length,
                Socket = (int)socket
            };

            // TODO: allow modifying wsa_send packets
            try { OnReceivePacket(packet); } catch { }

            WSASendHook.Continue();

            return length;
        }

        internal static int repl_wsa_recv(IntPtr socket, ref WSABuffer wsaBuffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine)
        {
            WSARecvHook.Suspend();

            var length = WSARecv(socket, ref wsaBuffer, bufferCount, out bytesTransferred, ref socketFlags, overlapped, completionRoutine);
            var buffer = wsaBuffer.Pointer.Copy(0, wsaBuffer.Length);

            var packet = new Packet()
            {
                Channel = PacketChannel.WSARecv,
                Buffer = buffer,
                Source = socket.GetSourceIPEndPoint(),
                Destination = socket.GetDestinationIPEndPoint(),
                Length = length,
                Socket = (int)socket
            };

            // TODO: allow modifying wsa_recv packets
            try { OnReceivePacket(packet); } catch { }

            WSARecvHook.Continue();
            return length;
        }

        [DllImport("WS2_32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int send(IntPtr socket, IntPtr buffer, int length, int flags);

        internal delegate int SendCallback(IntPtr socket, IntPtr buffer, int length, int flags);

        internal static SendCallback callback_repl_send = new SendCallback(repl_send);

        [DllImport("WS2_32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int recv(IntPtr socket, IntPtr buffer, int length, int flags);

        internal delegate int ReceiveCallback(IntPtr socket, IntPtr buffer, int length, int flags);

        internal static ReceiveCallback callback_repl_recv = new ReceiveCallback(repl_recv);

        [DllImport("WS2_32.dll", SetLastError = true)]
        internal static extern int sendto(IntPtr socketHandle, IntPtr pinnedBuffer, int len, int socketFlags, byte[] socketAddress, int socketAddressSize);

        internal delegate int SendToCallback(IntPtr socketHandle, IntPtr pinnedBuffer, int len, int socketFlags, byte[] socketAddress, int socketAddressSize);

        internal static SendToCallback callback_repl_send_to = new SendToCallback(repl_send_to);

        [DllImport("WS2_32.dll", SetLastError = true)]
        internal static extern int recvfrom(IntPtr socketHandle, IntPtr pinnedBuffer, int len, int socketFlags, ref byte[] socketAddress, ref int socketAddressSize);

        internal delegate int ReceiveFromCallback(IntPtr socketHandle, IntPtr pinnedBuffer, int len, int socketFlags, ref byte[] socketAddress, ref int socketAddressSize);

        internal static ReceiveFromCallback callback_repl_recv_from = new ReceiveFromCallback(repl_recv_from);

        [DllImport("WS2_32.dll", SetLastError = true)]
        internal static extern int WSASend(IntPtr socketHandle, ref WSABuffer buffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine);

        internal delegate int WSASendCallback(IntPtr socketHandle, ref WSABuffer buffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine);

        internal static WSASendCallback callback_repl_wsa_send = new WSASendCallback(repl_wsa_send);

        [DllImport("WS2_32.dll", SetLastError = true)]
        internal static extern int WSARecv(IntPtr socketHandle, ref WSABuffer buffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine);

        internal delegate int WSARecvCallback(IntPtr socketHandle, ref WSABuffer buffer, int bufferCount, out int bytesTransferred, ref int socketFlags, IntPtr overlapped, IntPtr completionRoutine);

        internal static WSARecvCallback callback_repl_wsa_recv = new WSARecvCallback(repl_wsa_recv);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WSABuffer
        {
            internal int Length; // length of buffer
            internal IntPtr Pointer; // pointer to buffer
        }
    }
}