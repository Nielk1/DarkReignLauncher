using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DarkHook
{
    /// <summary>
    /// EasyHook will look for a class implementing <see cref="EasyHook.IEntryPoint"/> during injection. This
    /// becomes the entry point within the target process after injection is complete.
    /// </summary>
    public class InjectionEntryPoint : EasyHook.IEntryPoint
    {
        /// <summary>
        /// Reference to the server interface within FileMonitor
        /// </summary>
        ServerInterface _server = null;

        /// <summary>
        /// Message queue of all files accessed
        /// </summary>
        Queue<string> _messageQueue = new Queue<string>();

        string BasePath;
        List<string> ModPaths;

        Dictionary<string, string> PathRedirectCache;

        /// <summary>
        /// EasyHook requires a constructor that matches <paramref name="context"/> and any additional parameters as provided
        /// in the original call to <see cref="EasyHook.RemoteHooking.Inject(int, EasyHook.InjectionOptions, string, string, object[])"/>.
        /// 
        /// Multiple constructors can exist on the same <see cref="EasyHook.IEntryPoint"/>, providing that each one has a corresponding Run method (e.g. <see cref="Run(EasyHook.RemoteHooking.IContext, string)"/>).
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public InjectionEntryPoint(EasyHook.RemoteHooking.IContext context, string channelName, List<string> ModPaths)
        {
            PathRedirectCache = new Dictionary<string, string>();

            BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.ModPaths = ModPaths;

            // Connect to server object using provided channel name
            _server = EasyHook.RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            _server.Ping();
        }

        /// <summary>
        /// The main entry point for our logic once injected within the target process. 
        /// This is where the hooks will be created, and a loop will be entered until host process exits.
        /// EasyHook requires a matching Run method for the constructor
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public void Run(
            EasyHook.RemoteHooking.IContext context,
            string channelName,
            List<string> ModPaths)
        {
            // Injection is now complete and the server interface is connected
            _server.IsInstalled(EasyHook.RemoteHooking.GetCurrentProcessId());

            // Install hooks

            // CreateFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
            var createFileHook = EasyHook.LocalHook.Create(
                EasyHook.LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"),
                new CreateFile_Delegate(CreateFile_Hook),
                this);

            // Activate hooks on all threads except the current thread
            createFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            _server.ReportMessage(context.HostPID, "CreateFile, ReadFile and WriteFile hooks installed");

            // Wake up the process (required if using RemoteHooking.CreateAndInject)
            EasyHook.RemoteHooking.WakeUpProcess();

            try
            {
                // Loop until FileMonitor closes (i.e. IPC fails)
                while (true)
                {
                    System.Threading.Thread.Sleep(500);

                    string[] queued = null;

                    lock (_messageQueue)
                    {
                        queued = _messageQueue.ToArray();
                        _messageQueue.Clear();
                    }

                    // Send newly monitored file accesses to FileMonitor
                    if (queued != null && queued.Length > 0)
                    {
                        _server.ReportMessages(context.HostPID, queued);
                    }
                    else
                    {
                        _server.Ping();
                    }
                }
            }
            catch
            {
                // Ping() or ReportMessages() will raise an exception if host is unreachable
            }

            // Remove hooks
            createFileHook.Dispose();

            // Finalise cleanup of hooks
            EasyHook.LocalHook.Release();
        }

        /// <summary>
        /// P/Invoke to determine the filename from a file handle
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364962(v=vs.85).aspx
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpszFilePath"></param>
        /// <param name="cchFilePath"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        #region CreateFileW Hook

        /// <summary>
        /// The CreateFile delegate, this is needed to create a delegate of our hook function <see cref="CreateFile_Hook(string, uint, uint, IntPtr, uint, uint, IntPtr)"/>.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
                    CharSet = CharSet.Unicode,
                    SetLastError = true)]
        delegate IntPtr CreateFile_Delegate(
                    String filename,
                    UInt32 desiredAccess,
                    UInt32 shareMode,
                    IntPtr securityAttributes,
                    UInt32 creationDisposition,
                    UInt32 flagsAndAttributes,
                    IntPtr templateFile);

        /// <summary>
        /// Using P/Invoke to call original method.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr CreateFileW(
            String filename,
            UInt32 desiredAccess,
            UInt32 shareMode,
            IntPtr securityAttributes,
            UInt32 creationDisposition,
            UInt32 flagsAndAttributes,
            IntPtr templateFile);

        /// <summary>
        /// The CreateFile hook function. This will be called instead of the original CreateFile once hooked.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        IntPtr CreateFile_Hook(
            String filename,
            UInt32 desiredAccess,
            UInt32 shareMode,
            IntPtr securityAttributes,
            UInt32 creationDisposition,
            UInt32 flagsAndAttributes,
            IntPtr templateFile)
        {
            if (!filename.StartsWith(@"\\"))
            {
                if (Path.IsPathRooted(filename))
                {
                    if (filename.StartsWith(BasePath))
                    {

                    }
                }
                else if (ModPaths.Count > 0)
                {
                    // unrooted paths are always asset loads
                    if (filename.StartsWith(@".\dark\"))
                    {
                        if(PathRedirectCache.ContainsKey(filename))
                        {
                            return CreateFileW(PathRedirectCache[filename], desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                        }
                        else
                        {
                            foreach (string modpath in ModPaths)
                            {
                                string newPath = filename.Replace(@".\dark\", $@".\mods\{modpath}\");
                                if (File.Exists(newPath))
                                {
                                    PathRedirectCache[filename] = newPath;
                                    try
                                    {
                                        lock (this._messageQueue)
                                        {
                                            if (this._messageQueue.Count < 1000)
                                            {
                                                this._messageQueue.Enqueue($"Redirected \"{filename}\" to \"{newPath}\"");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // swallow exceptions so that any issues caused by this code do not crash target process
                                    }
                                    return CreateFileW(newPath, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                                }
                            }

                            // no alternate file found, so cache to use the normal file for speed
                            PathRedirectCache[filename] = filename;
                        }
                    }
                }
            }
            return CreateFileW(filename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);


            /*{
                try
                {
                    lock (this._messageQueue)
                    {
                        if (this._messageQueue.Count < 1000)
                        {
                            string mode = string.Empty;
                            switch (creationDisposition)
                            {
                                case 1:
                                    mode = "CREATE_NEW";
                                    break;
                                case 2:
                                    mode = "CREATE_ALWAYS";
                                    break;
                                case 3:
                                    mode = "OPEN_ALWAYS";
                                    break;
                                case 4:
                                    mode = "OPEN_EXISTING";
                                    break;
                                case 5:
                                    mode = "TRUNCATE_EXISTING";
                                    break;
                            }

                            // Add message to send to FileMonitor
                            this._messageQueue.Enqueue(string.Format($"{mode}\t{filename}"));
                            //this._messageQueue.Enqueue(string.Format($"{mode}\t{checkPath}"));
                        }
                    }
                }
                catch
                {
                    // swallow exceptions so that any issues caused by this code do not crash target process
                }
            }

            // now call the original API...
            return CreateFileW(
                filename,
                desiredAccess,
                shareMode,
                securityAttributes,
                creationDisposition,
                flagsAndAttributes,
                templateFile);*/
        }

        #endregion
    }
}
