using DarkHook;
using EasyHook;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DarkReignBootstrap
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string CleanArguments = string.Empty;
                string ModFile = null;
                {
                    string Executable = Environment.GetCommandLineArgs()[0]; // Command invocation part
                    string RawCommandLine = Environment.CommandLine;         // Complete command
                    CleanArguments = RawCommandLine.Remove(RawCommandLine.IndexOf(Executable), Executable.Length).TrimStart('"').Substring(1);
                    ModFile = args[0];
                    CleanArguments = CleanArguments.Substring(ModFile.Length).TrimStart();
                }
                string ModFilePath = Path.Combine("ldata", ModFile + ".launchprofile");
                if (!string.IsNullOrWhiteSpace(ModFile) && File.Exists(ModFilePath))
                {
                    ModInstructions Script = new ModInstructions(ModFilePath);

                    Console.WriteLine($"DKREIGN.EXE {CleanArguments}");

                    /// Start method that doesn't cause wierd anet errors
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = @"..\DKREIGN.EXE",

                        // back up into the game's folder from our subfolder
                        WorkingDirectory = "..",

                        // pass the raw argument string after the mod/launch profile name
                        Arguments = CleanArguments,

                        // this is important to preserving the process chain so overlays like Steam work
                        UseShellExecute = false,
                    };

                    Process DarkReignProcess = Process.Start(info);

                    if (DarkReignProcess != null && !DarkReignProcess.HasExited)
                    {
                        Script.DoFunctionHook(DarkReignProcess);
                    }

                    // start and do the hook, causes an ANET thread error dialog that's anoying
                    //Process DarkReignProcess = Script.DoFunctionHook("DKREIGN.EXE", CleanArguments);

                    if (DarkReignProcess == null && DarkReignProcess.HasExited)
                    {
                        Console.WriteLine("Application did not start");
                        return;
                    }

                    // Wait 150 MS or till user input is possible.
                    // If the computer is so fast it puts the CD check up before 150 MS, it will stop waiting
                    // If the computer is so slow that it hasn't decompressed the injection will be overwritten by it
                    //DarkReignProcess.WaitForInputIdle(150);

                    // Write our ASM changes, hopefully after the decompression
                    //Script.DoAsmInjections(DarkReignProcess);

                    bool BrokeOutOfSubloop = false;
                    //for (; ; )
                    while (DarkReignProcess != null && !DarkReignProcess.HasExited)
                    {
                        if (DarkReignProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            // The main window is open, so we must have injected our ASM successfully
                            break;
                        }

                        try
                        {
                            foreach (var WindowHandle in DarkReignProcess.EnumerateWindowHandles())
                            {
                                StringBuilder Message = new StringBuilder(1000);
                                User32.GetClassName(WindowHandle, Message, Message.Capacity);
                                string MessageString = Message.ToString();

                                // Are we a dialog box?
                                if (MessageString == "#32770")
                                {
                                    // We see a dialog box, which means our ASM injection was too early or we disabled early injection
                                    // Apply the injections again
                                    Console.WriteLine("Injecting ASM");
                                    Script.DoAsmInjections(DarkReignProcess);
                                    Console.WriteLine("Injected ASM");

                                    short shiftKeyStatus = User32.GetKeyState(User32.VirtualKeyStates.VK_LSHIFT);
                                    if (shiftKeyStatus >= 0)
                                    {
                                        // Send the MessageBoxA the Yes button ID code, this will work reguardless of localization
                                        User32.SendMessage(WindowHandle, User32.WM_COMMAND, (User32.BN_CLICKED << 16) | User32.IDYES, IntPtr.Zero);
                                    }

                                    // Make sure our break out of this loop applies to the parent loop too
                                    BrokeOutOfSubloop = true;
                                    break;
                                }
                            }
                        }
                        catch (ArgumentException)
                        {
                            // The assumption here is that if this failed, it's because the process isn't open, so we should just give up
                            break;
                        }

                        // sublook broke out, so we can too
                        if (BrokeOutOfSubloop) break;

                        // loop throttle
                        Thread.Sleep(10);
                    }

                    DarkReignInterface intf = new DarkReignInterface(DarkReignProcess);

                    // let's wait for the process to exit by polling instead of using WaitForExit()
                    // this seems to work better if the process is already closed or does something strange
                    //Console.Clear();
                    while (DarkReignProcess != null && !DarkReignProcess.HasExited)
                    {
                        Thread.Sleep(100);
                        /*Console.SetCursorPosition(0, Console.WindowHeight - 69);
                        Console.Write(new string('-', Console.BufferWidth));
                        Console.Write($"Runcode: {intf.Runcode}".PadRight(Console.BufferWidth));
                        Console.Write($"GameType: {intf.GameType}".PadRight(Console.BufferWidth));
                        Console.Write($"InstantAction: {intf.InstantAction}\"".PadRight(Console.BufferWidth));
                        Console.Write($"ScenarioName: \"{intf.ScenarioName}\"".PadRight(Console.BufferWidth));
                        Console.Write($"ScenarioDir: \"{intf.ScenarioDir}\"".PadRight(Console.BufferWidth));
                        Console.Write($"ScenarioScn: \"{intf.ScenarioScn}\"".PadRight(Console.BufferWidth));
                        Console.Write($"ScenarioMap: \"{intf.ScenarioMap}\"".PadRight(Console.BufferWidth));
                        Console.Write($"ScenarioMm: \"{intf.ScenarioMm}\"".PadRight(Console.BufferWidth));
                        Console.Write($"TerrainName: \"{intf.TerrainName}\"".PadRight(Console.BufferWidth));
                        Console.Write($"Brightness: {intf.Brightness}".PadRight(Console.BufferWidth));
                        Console.Write($"Paused: {intf.Paused}".PadRight(Console.BufferWidth));
                        Console.Write($"InEditor: {intf.InEditor}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableFoundations: {intf.DisableFoundations}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableFog: {intf.DisableFog}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableBlack: {intf.DisableBlack}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableGiving: {intf.DisableGiving}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableViewally: {intf.DisableViewally}".PadRight(Console.BufferWidth));
                        Console.Write($"DisableAlliances: {intf.DisableAlliances}".PadRight(Console.BufferWidth));
                        Console.Write($"TechLevel: {intf.TechLevel:2}".PadRight(Console.BufferWidth));
                        Console.Write($"GameSpeed: {intf.GameSpeed}".PadRight(Console.BufferWidth));
                        Console.Write($"MyTeam: {intf.MyTeam}".PadRight(Console.BufferWidth));
                        Console.Write(new string('-', Console.BufferWidth));
                        Console.Write($"Team_Type: {intf.Team_Type}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_InitialType: {intf.Team_InitialType}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_PlayerName: \"{intf.Team_PlayerName}\"".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Group: {intf.Team_Group}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_MiniMap: {intf.Team_MiniMap}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Relation: {string.Join(",", intf.Team_Relation.Select(dr => dr))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_LineOfSight: {string.Join(",", intf.Team_LineOfSight.Select(dr => dr.ToString("X").PadLeft(8, '0')))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_SeeResource: {string.Join(",", intf.Team_SeeResource.Select(dr => dr.ToString("X").PadLeft(8, '0')))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Credits: {intf.Team_Credits}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_StartCredits: {intf.Team_StartCredits}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Resource_Suppply: {string.Join(",", intf.Team_Resource_Suppply.Select(dr => dr.ToString("X").PadLeft(8, '0')))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Resource_Usage:   {string.Join(",", intf.Team_Resource_Usage.Select(dr => dr.ToString("X").PadLeft(8, '0')))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Resource_Percent: {string.Join(",", intf.Team_Resource_Percent.Select(dr => dr.ToString("X").PadLeft(8, '0')))}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_CPU_Load: {intf.Team_CPU_Load}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_CPU_MaxLoad: {intf.Team_CPU_MaxLoad}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_CPU_Damage: {intf.Team_CPU_Damage}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_CPU_Percent: {intf.Team_CPU_Percent}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UnitCount: {intf.Team_UnitCount}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Side: {intf.Team_Side}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_NonDefault: {intf.Team_NonDefault}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Color: {intf.Team_Color}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_PalIndex: {intf.Team_PalIndex}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_KillEmAll: {intf.Team_KillEmAll}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_KillsUnits: {intf.Team_Stats_KillsUnits}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_KillsBuildings: {intf.Team_Stats_KillsBuildings}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_LossesUnits: {intf.Team_Stats_LossesUnits}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_LossesBuildings: {intf.Team_Stats_LossesBuildings}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_MadeUnits: {intf.Team_Stats_MadeUnits}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_MadeBuildings: {intf.Team_Stats_MadeBuildings}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_Stats_Collected: {intf.Team_Stats_Collected}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK1: {intf.Team_UNK1:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK2: {intf.Team_UNK2:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK3: {intf.Team_UNK3:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK4: {intf.Team_UNK4:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_StartPos_X: {intf.Team_StartPos_X}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_StartPos_Y: {intf.Team_StartPos_Y}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UnitDef_Autonomy: {intf.Team_UnitDef_Autonomy}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UnitDef_Tenacit: {intf.Team_UnitDef_Tenacit}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UnitDef_SelfPres: {intf.Team_UnitDef_SelfPres}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK5: {intf.Team_UNK5:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK6: {intf.Team_UNK6:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK7: {intf.Team_UNK7:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK8: {intf.Team_UNK8:X8}".PadRight(Console.BufferWidth));
                        Console.Write($"Team_UNK9: {intf.Team_UNK9:X8}".PadRight(Console.BufferWidth));
                        //Console.Write($"TeamData: {BitConverter.ToString(intf.TeamData).Replace("-", string.Empty)}".PadRight(Console.BufferWidth));
                        Console.Write(new string('-', Console.BufferWidth));*/
                    }

                    //Console.ReadKey(true);

                    return;
                }
            }

        }
    }
}
