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
        public string Filename { get; set; }
        public string Title { get; set; }
        public string Sort { get; set; }
        public string Note { get; set; }
        public string Logo { get; set; }
        public string Background { get; set; }
        public bool TopMenu { get; set; }
        public List<Tuple<IntPtr, byte[]>> AsmInjections { get; set; }
        public List<string> ModPaths { get; set; }
        public List<string> BlockDirs { get; set; }
        public string SaveFolder { get; set; }
        public List<ModOption> Options { get; set; }

        public ModInstructions(string ModFilePath)
        {
            Options = new List<ModOption>();

            Filename = Path.GetFileNameWithoutExtension(ModFilePath);

            string FilenameOptions = @"ldata\" + Filename + ".opt";
            HashSet<string> OptionCheck = new HashSet<string>();
            if (File.Exists(FilenameOptions))
            {
                foreach(string line in File.ReadAllLines(FilenameOptions))
                {
                    if (line.Length > 0)
                        OptionCheck.Add(line);
                }
            }

            Title = Path.GetFileNameWithoutExtension(ModFilePath);
            AsmInjections = new List<Tuple<IntPtr, byte[]>>();
            ModPaths = new List<string>();
            BlockDirs = new List<string>();
            SaveFolder = null;

            string[] lines = File.ReadAllLines(ModFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] lineParts = lines[i].Split(new char[] { '\t' });

                if(lineParts[0] == "OPT")
                {
                    if(lineParts.Length > 4)
                    {
                        if (OptionCheck.Contains(lineParts[1]))
                        {
                            Options.Add(new ModOption()
                            {
                                ID = lineParts[1],
                                Title = lineParts[2],
                                Parent = this,
                                Active = true,
                            });

                            // we are an option that is active, that means we skip the first 3 data blocks which are the magic string, optionid, and description
                            lineParts = lineParts.Skip(3).ToArray();
                        }
                        else
                        {
                            Options.Add(new ModOption()
                            {
                                ID = lineParts[1],
                                Title = lineParts[2],
                                Parent = this,
                                Active = false,
                            });

                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

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
                    case "MENU":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                TopMenu = lineParts[1].ToUpperInvariant() == "TOP";
                            }
                        }
                        break;
                    case "SORT":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                Sort = lineParts[1];
                            }
                        }
                        break;
                    case "NOTE":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                Note = lineParts[1];
                            }
                        }
                        break;
                    case "LOGO":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                Logo = lineParts[1];
                            }
                        }
                        break;
                    case "BACK":
                        {
                            if (lineParts.Length > 1 && !string.IsNullOrWhiteSpace(lineParts[1]))
                            {
                                Background = lineParts[1];
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
                                        if (!asmLineParts[0].ToUpperInvariant().Any(dr => !new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' }.Contains(dr)))
                                        {
                                            try
                                            {
                                                int addr = Convert.ToInt32(asmLineParts[0], 16);
                                                byte[] data = StringToByteArray(asmLineParts[1]);

                                                AsmInjections.Add(new Tuple<IntPtr, byte[]>(new IntPtr(addr), data));
                                            }
                                            catch { }
                                        }
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
                                if (Directory.Exists(Path.Combine("..", ModFolder)))
                                {
                                    ModPaths.Add(lineParts[1]);
                                }
                            }
                            catch { }
                        }
                        break;
                    /*case "BLOCKDIR":
                        {
                            try
                            {
                                BlockDirs.Add(lineParts[1].ToLowerInvariant().TrimEnd('\\') + @"\");
                            }
                            catch { }
                        }
                        break;*/
                }
            }
        }

        /// <summary>
        /// Note that this does not activate the option in this instance but simply saves to disk it should be active next time we construct
        /// To make this actually update would require rescanning the launcherprofile or tracking that it is optional instead of skipping disabled optional items
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public void SetOption(string id, bool val)
        {
            string FilenameOptions = @"ldata\" + Filename + ".opt";
            HashSet<string> OptionCheck = new HashSet<string>();
            if (File.Exists(FilenameOptions))
            {
                foreach (string line in File.ReadAllLines(FilenameOptions))
                {
                    if (line.Length > 0)
                        OptionCheck.Add(line);
                }
            }
            if(OptionCheck.Contains(id))
            {
                if (!val) OptionCheck.Remove(id);
            }
            else
            {
                if (val) OptionCheck.Add(id);
            }

            File.WriteAllLines(FilenameOptions, OptionCheck.ToArray());
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public void DoAsmInjections(Process proc)
        {
            proc.Suspend();
            if (AsmInjections.Count > 0)
            {
                IntPtr ret = Kernel32.OpenProcess(0x1F0FFF, false, proc.Id);

                foreach (Tuple<IntPtr, byte[]> asm in AsmInjections)
                {
                    Memory.WriteMemory(ret, asm.Item1, asm.Item2);
                }
            }
            proc.Resume();
        }

        public Process DoFunctionHook(string proc, string args)
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
                Console.WriteLine("Attempting to create and inject into process");
                int procID = 0;

                // inject into existing process
                EasyHook.RemoteHooking.CreateAndInject(
                    proc,
                    args,
                    0,
                    injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                    injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                    out procID,
                    channelName,        // the parameters to pass into injected library
                    ModPaths,
                    BlockDirs,
                    SaveFolder ?? string.Empty
                );

                if (procID > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Application ID found, ID is {procID}");
                    Console.ResetColor();
                    return Process.GetProcessById(procID);
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Application ID not found");
                Console.ResetColor();
                return null;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ResetColor();
                Console.WriteLine(e.ToString());
            }
            return null;
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
                        BlockDirs,
                        SaveFolder ??string.Empty
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

    public class ModOption
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public bool Active { get; set; }

        public ModInstructions Parent { get; set; }

        public void Set(bool Active)
        {
            Parent.SetOption(ID, Active);
        }
    }
}