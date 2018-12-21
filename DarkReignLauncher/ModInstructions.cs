using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DarkReignLauncher
{
    public class ModInstructions
    {
        public List<Tuple<IntPtr, byte[]>> AsmInjections { get; set; }

        public ModInstructions(string ModFilePath)
        {
            AsmInjections = new List<Tuple<IntPtr, byte[]>>();

            string[] lines = File.ReadAllLines(ModFilePath);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] lineParts = lines[i].Split(new char[] { '\t' });
                switch (lineParts[0])
                {
                    case "ASM":
                        {
                            string AsmFile = Path.Combine("ldata", lineParts[1] + ".asmpatch");
                            if (File.Exists(AsmFile))
                            {
                                string[] asmLines = File.ReadAllLines(AsmFile);
                                foreach (string asmLine in asmLines)
                                {
                                    string[] asmLineParts = asmLine.Split(new char[] { '\t' }, 3);
                                    int addr = Convert.ToInt32(asmLineParts[0], 16);
                                    byte[] data = StringToByteArray(asmLineParts[1]);

                                    AsmInjections.Add(new Tuple<IntPtr, byte[]>(new IntPtr(addr), data));
                                }
                            }
                        }
                        break;
                }
            }
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private bool WriteMemory(IntPtr pid, IntPtr lpBaseAddress, byte[] value)
        {
            return Kernel32.WriteProcessMemory(pid, lpBaseAddress, value, Marshal.SizeOf<byte>() * value.Length, out var bytesread);
        }

        public void DoAsmInjections(Process proc)
        {
            proc.Suspend();
            if (AsmInjections.Count > 0)
            {
                IntPtr ret = Kernel32.OpenProcess(0x1F0FFF, false, proc.Id);

                foreach (Tuple<IntPtr, byte[]> asm in AsmInjections)
                {
                    WriteMemory(ret, asm.Item1, asm.Item2);
                }
            }
            proc.Resume();
        }
    }
}