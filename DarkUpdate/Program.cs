using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DarkUpdate
{
    static class Program
    {
        static UpdateData data;
        static StreamWriter writer;
        static WebClient client;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)(MySecurityProtocolType.Tls12 | MySecurityProtocolType.Tls11 | MySecurityProtocolType.Tls);
            client = new WebClient();

            using (FileStream str = new FileStream("DarkUpdate.log", FileMode.Create))
            using (writer = new StreamWriter(str))
            {
                Console.Title = "Dark Reign Updater " + Version;

                WriteLine("Testing 7zip");
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "7za.exe",
                        UseShellExecute = false,
                    }).WaitForExit();
                }
                catch
                {
                    WriteDangerLine("Please install 7zip or place 7za.exe in the same folder as this this program");
                    ConsolePause("Press any key to exit.");
                    return;
                }

                WriteLine("Welcome to the Dark Reign Community Patch Updater.");
                if (!Directory.Exists("DarkUpdate")) Directory.CreateDirectory("DarkUpdate");
                DownloadMetadata();
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(UpdateData));
                    data = (UpdateData)serializer.Deserialize(File.OpenRead(Path.Combine("DarkUpdate", "update.xml")));
                }

                {
                    int[] fileVersion = data.Version.Split('.').Select(dr => int.Parse(dr)).ToArray();
                    int[] programVersion = Version.Split('.').Select(dr => int.Parse(dr)).ToArray();

                    bool ok = true;
                    for(int i=0;i<4;i++)
                    {
                        if(fileVersion[i] > programVersion[i])
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (!ok)
                    {
                        WriteDangerLine("Updater is out of date.");
                        WriteDangerLine($"Updater: {Version}");
                        WriteDangerLine($"Metadata: {data.Version}");
                        ConsolePause("Please download an updated version and try again.");
                        return;
                    }
                }

                WriteLine("Checking local directory for Dark Reign resources");
                if (!File.Exists("DKREIGN.EXE"))
                {
                    WriteLine("DKREIGN.EXE not found.");
                    CopyDarkReign();
                }
                else
                {
                    WriteLine("DKREIGN.EXE found, scanning for additional files");
                    if (!CheckFiles())
                    {
                        WriteLine("Some files not found.");
                        CopyDarkReign();
                    }
                }

                DeleteFiles();

                CleanOldModules();

                DownloadNewModules();

                WriteLine("Starting Launcher");

                Process.Start(new ProcessStartInfo(){
                    FileName = Path.Combine("launcher", "DarkReignLauncher.exe"),
                    WorkingDirectory = "launcher",
                    UseShellExecute = false,
                });
                //ConsolePause("Debug Pause");
            }
        }

        public static string Version
        {
            get
            {
                Assembly assem = Assembly.GetExecutingAssembly();
                AssemblyName assemName = assem.GetName();
                Version ver = assemName.Version;
                return ver.ToString();
            }
        }

        static void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
            writer.WriteLine($"\t{format}", arg);
        }
        static void WriteGoodLine(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(format, arg);
            Console.ResetColor();
            writer.WriteLine($"G\t{format}", arg);
        }
        static void WriteWarnLine(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(format, arg);
            Console.ResetColor();
            writer.WriteLine($"W\t{format}", arg);
        }
        static void WriteDangerLine(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg);
            Console.ResetColor();
            writer.WriteLine($"D\t{format}", arg);
        }
        static void ConsolePause(string message)
        {
            Console.WriteLine(message);
            ConsoleKeyInfo key = Console.ReadKey(true);
            writer.WriteLine($"P\t{message}");
            writer.WriteLine($"K\t{Enum.GetName(typeof(ConsoleKey), key.Key)}");
        }

        [Flags]
        private enum MySecurityProtocolType
        {
            //
            // Summary:
            //     Specifies the Secure Socket Layer (SSL) 3.0 security protocol.
            Ssl3 = 48,
            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.0 security protocol.
            Tls = 192,
            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.1 security protocol.
            Tls11 = 768,
            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.2 security protocol.
            Tls12 = 3072
        }
        static void DownloadMetadata()
        {
            WriteLine("Downloading Metadata");
            if (File.Exists(Path.Combine("DarkUpdate", "update.xml.new"))) File.Delete(Path.Combine("DarkUpdate", "update.xml.new"));
            client.DownloadFile("https://darkreign.iondriver.com/patch/update.xml", Path.Combine("DarkUpdate", "update.xml.new"));
            if (File.Exists(Path.Combine("DarkUpdate", "update.xml.old"))) File.Delete(Path.Combine("DarkUpdate", "update.xml.old"));
            if (File.Exists(Path.Combine("DarkUpdate", "update.xml"))) File.Move(Path.Combine("DarkUpdate", "update.xml"), Path.Combine("DarkUpdate", "update.xml.old"));
            File.Move(Path.Combine("DarkUpdate", "update.xml.new"), Path.Combine("DarkUpdate", "update.xml"));
            WriteLine("Download Complete");
        }

        static void CleanOldModules()
        {
            WriteLine($"Checking Old Modules");
            foreach (string file in data.OldModules)
            {
                if (file.Contains("..") || Path.IsPathRooted(file))
                {
                    WriteDangerLine($"Possibly unsafe delete detected, skipping delete of \"{file}\"");
                    continue;
                }
                if (File.Exists(Path.Combine("DarkUpdate", file)))
                {
                    WriteDangerLine($"Deleting File \"{file}\"");
                    File.Delete(Path.Combine("DarkUpdate", file));
                }
                if (File.Exists(Path.Combine("DarkUpdate", file + ".installed")))
                {
                    WriteDangerLine($"Deleting File \"{file + ".installed"}\"");
                    File.Delete(Path.Combine("DarkUpdate", file + ".installed"));
                }
            }
            WriteLine($"Done Checking Old Modules");
        }

        static void DownloadNewModules()
        {
            WriteLine($"Checking Modules");
            foreach (string file in data.Modules)
            {
                if (File.Exists(Path.Combine("DarkUpdate", file))) File.Delete(Path.Combine("DarkUpdate", file));

                if (!File.Exists(Path.Combine("DarkUpdate", file + ".installed")))
                {
                    WriteWarnLine($"Downloading Module \"{file}\"");
                    client.DownloadFile("https://darkreign.iondriver.com/patch/" + file, Path.Combine("DarkUpdate", file));
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "7za.exe",
                        //WorkingDirectory = Directory.GetCurrentDirectory(),
                        Arguments = $"x -aoa \"{Path.Combine("DarkUpdate", file)}\"",
                        UseShellExecute = false,
                    }).WaitForExit();
                    //WriteLine($"x -aoa \"{Path.Combine("DarkUpdate", file)}\"");
                    //File.Delete(Path.Combine("DarkUpdate", file));
                    //File.CreateText(Path.Combine("DarkUpdate", file + ".installed")).Close();
                    File.Move(Path.Combine("DarkUpdate", file), Path.Combine("DarkUpdate", file + ".installed"));
                }
            }
            WriteLine($"Done Checking Modules");
        }

        static void DeleteFiles()
        {
            WriteLine($"Checking Delete Files");
            foreach (string file in data.DeleteFiles)
            {
                if(file.Contains("..") || Path.IsPathRooted(file))
                {
                    WriteDangerLine($"Possibly unsafe delete detected, skipping delete of \"{file}\"");
                    continue;
                }

                if (File.Exists(file))
                {
                    WriteDangerLine($"Deleting File \"{file}\"");
                    File.Delete(file);
                }
            }
            WriteLine($"Done Checking Delete Files");
        }

        static bool CheckFiles()
        {
            foreach(string file in data.BaseFiles)
            {
                if (!File.Exists(file))
                {
                    WriteDangerLine($"File Not Found \"{file}\"");
                    return false;
                }
                WriteGoodLine($"File Found \"{file}\"");
            }
            return true;
        }

        static void CopyDarkReign()
        {
            if(File.Exists("darkcore.zip"))
            {
                WriteLine("darkcore.zip detected. Attempting full install. If darkcore.zip is in error, please delete it and try again.");
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "7za.exe",
                    //WorkingDirectory = Directory.GetCurrentDirectory(),
                    Arguments = $"x -aoa \"darkcore.zip\"",
                    UseShellExecute = false,
                }).WaitForExit();
                WriteLine("Extract complete.");
                return;
            }
            WriteLine("Please select DKREIGN.EXE from a 1.4 installation to copy into this location.");
            ConsolePause("Press any key to select DKREIGN.EXE");
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "DKREIGN.EXE|DKREIGN.EXE";
            if(dlg.ShowDialog() == DialogResult.OK)
            {
                string baseDir = Path.GetDirectoryName(dlg.FileName);

                foreach (string file in data.BaseFiles)
                {
                    //if (File.Exists(file))
                    //    File.Delete(file);
                    if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(file)) && !Directory.Exists(Path.GetDirectoryName(file))) Directory.CreateDirectory(Path.GetDirectoryName(file));
                    if (File.Exists(file))
                    {
                        WriteWarnLine($"File Already Present \"{file}\"");
                    }
                    else
                    if (File.Exists(Path.Combine(baseDir, file)))
                    {
                        WriteGoodLine($"File Found \"{file}\"");
                        File.Copy(Path.Combine(baseDir, file), file);
                    }
                    else
                    {
                        WriteDangerLine($"File Not Found \"{file}\"");
                    }
                }
            }
        }
    }
}
