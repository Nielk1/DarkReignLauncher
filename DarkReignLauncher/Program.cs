using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DarkReignLauncher
{
    class Program
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          byte[] lpBuffer,
          Int32 nSize,
          out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          [MarshalAs(UnmanagedType.AsAny)] object lpBuffer,
          int dwSize,
          out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string exe = Environment.GetCommandLineArgs()[0]; // Command invocation part
                string rawCmd = Environment.CommandLine;          // Complete command
                string argsOnly = rawCmd.Remove(rawCmd.IndexOf(exe), exe.Length).TrimStart('"').Substring(1);
                string modFile = args[0];
                argsOnly = argsOnly.Substring(modFile.Length).TrimStart();
                string modFilename = Path.Combine("launcher", modFile + ".modification");
                if (!string.IsNullOrWhiteSpace(modFile) && File.Exists(modFilename))
                {
                    List<Tuple<IntPtr, byte[]>> Inject = new List<Tuple<IntPtr, byte[]>>();

                    string[] lines = File.ReadAllLines(modFilename);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] lineParts = lines[i].Split(new char[] { '\t' });
                        switch (lineParts[0])
                        {
                            case "ASM":
                                {
                                    string AsmFile = Path.Combine("launcher", lineParts[1] + ".asmpatch");
                                    if (File.Exists(AsmFile))
                                    {
                                        string[] asmLines = File.ReadAllLines(AsmFile);
                                        foreach (string asmLine in asmLines)
                                        {
                                            string[] asmLineParts = asmLine.Split(new char[] { '\t' }, 3);
                                            int addr = Convert.ToInt32(asmLineParts[0], 16);
                                            byte[] data = StringToByteArray(asmLineParts[1]);

                                            Inject.Add(new Tuple<IntPtr, byte[]>(new IntPtr(addr), data));
                                        }
                                    }
                                }
                                break;
                        }
                    }

                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = "DKREIGN.EXE",
                        Arguments = argsOnly,
                        UseShellExecute = false,
                    };

                    Process proc = Process.Start(info);

                    proc.WaitForInputIdle(100);
                    //proc.WaitForInputIdle();

                    SuspendProcess(proc);

                    if (Inject.Count > 0)
                    {
                        IntPtr ret = OpenProcess(0x1F0FFF, false, proc.Id);

                        foreach (Tuple<IntPtr, byte[]> asm in Inject)
                        {
                            WriteMemory(ret, asm.Item1, asm.Item2);
                        }
                    }

                    ResumeProcess(proc);

                    proc.WaitForExit();

                    return;
                }
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static bool WriteMemory(IntPtr pid, IntPtr lpBaseAddress, byte[] value)
        {
            return WriteProcessMemory(pid, lpBaseAddress, value, Marshal.SizeOf<byte>() * value.Length, out var bytesread);
        }

        private static void SuspendProcess(Process process)
        {
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(Process process)
        {
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }
    }
}
