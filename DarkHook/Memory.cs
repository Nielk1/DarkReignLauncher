using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DarkHook
{
    public class Memory
    {
        public static bool WriteMemory(IntPtr pid, IntPtr lpBaseAddress, byte[] value)
        {
            //return Kernel32.WriteProcessMemory(pid, lpBaseAddress, value, Marshal.SizeOf<byte>() * value.Length, out var bytesread);
            return Kernel32.WriteProcessMemory(pid, lpBaseAddress, value, Marshal.SizeOf(typeof(byte)) * value.Length, out var bytesread);
        }

        public static byte[] ReadMemory(IntPtr pid, IntPtr lpBaseAddress, int size)
        {
            byte[] buffer = new byte[size];
            IntPtr lpNumberOfBytesRead;
            bool success = Kernel32.ReadProcessMemory(pid, lpBaseAddress, buffer, size, out lpNumberOfBytesRead);

            return buffer;
        }

        public static UInt32 ReadMemoryUInt32(IntPtr pid, IntPtr lpBaseAddress)
        {
            return BitConverter.ToUInt32(ReadMemory(pid, lpBaseAddress, 4), 0);
        }

        public static Int32 ReadMemoryInt32(IntPtr pid, IntPtr lpBaseAddress)
        {
            return BitConverter.ToInt32(ReadMemory(pid, lpBaseAddress, 4), 0);
        }

        public static byte ReadMemoryByte(IntPtr pid, IntPtr lpBaseAddress)
        {
            return ReadMemory(pid, lpBaseAddress, 1)[0];
        }

        public static string ReadMemoryAsciiString(IntPtr pid, IntPtr lpBaseAddress, int length)
        {
            byte[] raw = ReadMemory(pid, lpBaseAddress, length);
            string rawStr = ASCIIEncoding.ASCII.GetString(raw);
            return rawStr.Substring(0, rawStr.IndexOf('\0'));
        }
    }
}
