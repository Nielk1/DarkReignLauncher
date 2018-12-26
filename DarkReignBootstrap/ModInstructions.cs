using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DarkReignBootstrap
{
    public class ModInstructions
    {
        public string Title { get; set; }
        public List<Tuple<IntPtr, byte[]>> AsmInjections { get; set; }
        public List<string> ModPaths { get; set; }
        public string SaveFolder { get; set; }

        public ModInstructions(string ModFilePath)
        {
            Title = Path.GetFileNameWithoutExtension(ModFilePath);
            AsmInjections = new List<Tuple<IntPtr, byte[]>>();
            ModPaths = new List<string>();
            SaveFolder = null;

            string[] lines = File.ReadAllLines(ModFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] lineParts = lines[i].Split(new char[] { '\t' });
                switch (lineParts[0])
                {
                    case "TITLE":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                Title = lineParts[1];
                            }
                        }
                        break;
                    case "SAVE":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                SaveFolder = lineParts[1];
                            }
                        }
                        break;
                    case "ASM":
                        {
                            string AsmFile = Path.Combine("ldata", lineParts[1] + ".asmpatch");
                            if (File.Exists(AsmFile))
                            {
                                string[] asmLines = File.ReadAllLines(AsmFile);
                                foreach (string asmLine in asmLines)
                                {
                                    string[] asmLineParts = asmLine.Split(new char[] { '\t' }, 3);
                                    if (asmLineParts.Length > 1) // at least 2 parts
                                    {
                                        int addr = Convert.ToInt32(asmLineParts[0], 16);
                                        byte[] data = StringToByteArray(asmLineParts[1]);

                                        AsmInjections.Add(new Tuple<IntPtr, byte[]>(new IntPtr(addr), data));
                                    }
                                }
                            }
                        }
                        break;
                    case "MOD":
                        {
                            try
                            {
                                string ModFolder = Path.Combine("mods", lineParts[1]);
                                if (Directory.Exists(ModFolder))
                                {
                                    ModPaths.Add(lineParts[1]);
                                }
                            }
                            catch { }
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

        public void DoFunctionHook(Process proc)
        {
            // Will contain the name of the IPC server channel
            string channelName = null;

            // Create the IPC server using the FileMonitorIPC.ServiceInterface class as a singleton
            EasyHook.RemoteHooking.IpcCreateServer<DarkHook.ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DarkHook.dll");

            try
            {
                // Injecting into existing process by Id
                if (proc.Id > 0)
                {
                    Console.WriteLine("Attempting to inject into process {0}", proc.Id);

                    // inject into existing process
                    EasyHook.RemoteHooking.Inject(
                        proc.Id,            // ID of process to inject into
                        injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                        injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                        channelName,        // the parameters to pass into injected library
                        ModPaths,
                        SaveFolder??string.Empty
                    );
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ResetColor();
                Console.WriteLine(e.ToString());
            }
        }
    }
}