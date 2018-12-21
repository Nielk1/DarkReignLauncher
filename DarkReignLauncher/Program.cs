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
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string CleanArguments = string.Empty;
                string ModFile = null;
                {
                    string Executable = Environment.GetCommandLineArgs()[0]; // Command invocation part
                    string RawCommandLine = Environment.CommandLine;          // Complete command
                    CleanArguments = RawCommandLine.Remove(RawCommandLine.IndexOf(Executable), Executable.Length).TrimStart('"').Substring(1);
                    ModFile = args[0];
                    CleanArguments = CleanArguments.Substring(ModFile.Length).TrimStart();
                }
                string ModFilePath = Path.Combine("ldata", ModFile + ".modification");
                if (!string.IsNullOrWhiteSpace(ModFile) && File.Exists(ModFilePath))
                {
                    ModInstructions Script = new ModInstructions(ModFilePath);

                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = "DKREIGN.EXE",

                        // pass the raw argument string after the mod/launch profile name
                        Arguments = CleanArguments,

                        // this is important to preserving the process chain so overlays like Steam work
                        UseShellExecute = false,
                    };

                    Process DarkReignProcess = Process.Start(info);

                    // Wait 150 MS or till user input is possible.
                    // If the computer is so fast it puts the CD check up before 150 MS, it will stop waiting
                    // If the computer is so slow that it hasn't decompressed the injection will be overwritten by it
                    DarkReignProcess.WaitForInputIdle(150);

                    // Write our ASM changes, hopefully after the decompression
                    Script.DoAsmInjections(DarkReignProcess);

                    bool BrokeOutOfSubloop = false;
                    for (; ; )
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
                                    // We see a dialog box, which means our ASM injection was too early
                                    // Apply the injections again
                                    Script.DoAsmInjections(DarkReignProcess);

                                    // Send the MessageBoxA the Yes button ID code, this will work reguardless of localization
                                    User32.SendMessage(WindowHandle, User32.WM_COMMAND, (User32.BN_CLICKED << 16) | User32.IDYES, IntPtr.Zero);

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

                    // let's wait for the process to exit by polling instead of using WaitForExit()
                    // this seems to work better if the process is already closed or does something strange
                    while (DarkReignProcess != null && !DarkReignProcess.HasExited)
                    {
                        Thread.Sleep(1000);
                    }

                    return;
                }
            }
        }



        

        





        
    }
}
