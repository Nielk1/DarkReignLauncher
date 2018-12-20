using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launch_Stock
{
    class Program
    {
        static void Main(string[] args)
        {
            string exe = Environment.GetCommandLineArgs()[0]; // Command invocation part
            string rawCmd = Environment.CommandLine;          // Complete command
            string argsOnly = rawCmd.Remove(rawCmd.IndexOf(exe), exe.Length).TrimStart('"').Substring(1);

            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "bootstrap.exe",
                Arguments = "dkreign " + argsOnly,
                UseShellExecute = false,
            };

            Process proc = Process.Start(info);
            proc.WaitForExit();

        }
    }
}
