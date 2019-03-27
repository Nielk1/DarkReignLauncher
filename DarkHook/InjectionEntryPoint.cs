using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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
        Queue<string> _screenshotQueue = new Queue<string>();

        string BasePath;
        List<string> ModPaths;
        string SaveFolder;

        Dictionary<string, string> PathRedirectCache;
        Dictionary<IntPtr, FindFileMeta> FindFileOverides;
        Dictionary<IntPtr, string> ScreenshotFiles;

        FileStream volSave;
        byte VolSet = 127;
        byte VolLast = 127;

        object DarkHookLog = new object();

        /// <summary>
        /// EasyHook requires a constructor that matches <paramref name="context"/> and any additional parameters as provided
        /// in the original call to <see cref="EasyHook.RemoteHooking.Inject(int, EasyHook.InjectionOptions, string, string, object[])"/>.
        /// 
        /// Multiple constructors can exist on the same <see cref="EasyHook.IEntryPoint"/>, providing that each one has a corresponding Run method (e.g. <see cref="Run(EasyHook.RemoteHooking.IContext, string)"/>).
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public InjectionEntryPoint(EasyHook.RemoteHooking.IContext context, string channelName, List<string> ModPaths, string SaveFolder)
        {
            PathRedirectCache = new Dictionary<string, string>();
            FindFileOverides = new Dictionary<IntPtr, FindFileMeta>();
            ScreenshotFiles = new Dictionary<IntPtr, string>();

            BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";
            this.ModPaths = ModPaths;
            this.SaveFolder = SaveFolder;

            VolSet = 127;
            volSave = File.Open("vol.bin", FileMode.OpenOrCreate);
            int volInt = volSave.ReadByte();
            if (volInt > -1) VolSet = (byte)volInt;
            volInt = volSave.ReadByte();
            volSave.SetLength(0);
            volSave.WriteByte(VolLast);
            volSave.WriteByte(VolSet);

            // Connect to server object using provided channel name
            _server = EasyHook.RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            _server.Ping();
        }

        private string CreateFileW_Lib;
        private string CloseHandle_Lib;
        private string FindFirstFileExW_Lib;
        private string FindNextFileW_Lib;
        private string FindClose_Lib;
        private IntPtr GetProcAddress(out string libUsed, string funcion, params string[] libs)
        {
            foreach (string lib in libs)
            {
                IntPtr func = EasyHook.LocalHook.GetProcAddress(lib, funcion);
                libUsed = lib;
                if (func != IntPtr.Zero)
                    return func;
            }
            libUsed = null;
            return IntPtr.Zero;
        }

        /// <summary>
        /// The main entry point for our logic once injected within the target process. 
        /// This is where the hooks will be created, and a loop will be entered until host process exits.
        /// EasyHook requires a matching Run method for the constructor
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public void Run(EasyHook.RemoteHooking.IContext context, string channelName, List<string> ModPaths, string SaveFolder)
        {
            //System.Threading.Thread.Sleep(1000 * 20); // time to attach debugger

            // Injection is now complete and the server interface is connected
            _server.IsInstalled(EasyHook.RemoteHooking.GetCurrentProcessId());

            File.WriteAllText("darkhook.log", "DarkHook Log " + DateTime.Now + "\r\n");

            // Install hooks
            // CreateFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
            var createFileHook = EasyHook.LocalHook.Create(GetProcAddress(out CreateFileW_Lib, "CreateFileW", "kernelbase.dll", "kernel32.dll"), new CreateFile_Delegate(CreateFile_Hook), this);
            var closeHandleHook = EasyHook.LocalHook.Create(GetProcAddress(out CloseHandle_Lib, "CloseHandle", "kernelbase.dll", "kernel32.dll"), new CloseHandle_Delegate(CloseHandle_Hook), this);
            ////var deleteFileHook = EasyHook.LocalHook.Create(GetProcAddress(out CreateFileW_Lib, "kernelbase.dll", "DeleteFileW", "kernel32.dll", "DeleteFileW"), new DeleteFile_Delegate(DeleteFile_Hook), this);
            var getPrivateProfileStringHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "GetPrivateProfileStringA"), new GetPrivateProfileString_Delegate(GetPrivateProfileString_Hook), this);
            var findFirstFileExHook = EasyHook.LocalHook.Create(GetProcAddress(out FindFirstFileExW_Lib, "FindFirstFileExW", "kernelbase.dll", "kernel32.dll"), new FindFirstFileExW_Delegate(FindFirstFileExW_Hook), this);
            var findNextFileWHook = EasyHook.LocalHook.Create(GetProcAddress(out FindNextFileW_Lib, "FindNextFileW", "kernelbase.dll", "kernel32.dll"), new FindNextFileW_Delegate(FindNextFileW_Hook), this);
            var findCloseHook = EasyHook.LocalHook.Create(GetProcAddress(out FindClose_Lib, "FindClose", "kernelbase.dll", "kernel32.dll"), new FindClose_Delegate(FindClose_Hook), this);
            var _accessHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("msvcrt.dll", "_access"), new _access_Delegate(_access_Hook), this);
            var AIL_redbook_volumeHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("mss32.dll", "_AIL_redbook_volume@4"), new AIL_redbook_volume_Delegate(AIL_redbook_volume_Hook), this);
            var AIL_redbook_set_volumeeHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("mss32.dll", "_AIL_redbook_set_volume@8"), new AIL_redbook_set_volume_Delegate(AIL_redbook_set_volume_Hook), this);

            // Activate hooks on all threads except the current thread
            createFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            closeHandleHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            ////deleteFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            getPrivateProfileStringHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileExHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findNextFileWHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findCloseHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            _accessHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            AIL_redbook_volumeHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            AIL_redbook_set_volumeeHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            _server.ReportMessage(context.HostPID, $@"Function hooks installed:
{CreateFileW_Lib} :: CreateFileW - File redirection
{CloseHandle_Lib} :: CloseHandle - File redirection
kernel32.dll :: GetPrivateProfileStringA - Overriding packman reads
{FindFirstFileExW_Lib} :: FindFirstFileExW - File redirection, access virtual file pool
{FindNextFileW_Lib} :: FindNextFileW - File redirection, access virtual file pool
{FindClose_Lib} :: FindClose - File redirection, access virtual file pool
msvcrt.dll :: _access - File redirection, confirming file is readable
mss32.dll :: _AIL_redbook_volume@4 - Redirect music get volume request
mss32.dll :: _AIL_redbook_set_volume@8 - Redirect music set volume request");


            _server.ReportMessage(context.HostPID, $"Mod Paths:\r\n{string.Join("\r\n", this.ModPaths)}");
            _server.ReportMessage(context.HostPID, $"Save Path: {this.SaveFolder}");

            // Wake up the process (required if using RemoteHooking.CreateAndInject)
            EasyHook.RemoteHooking.WakeUpProcess();

            try
            {
                // Loop until FileMonitor closes (i.e. IPC fails)
                while (true)
                {
                    System.Threading.Thread.Sleep(500);

                    {
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
                            continue;
                        }
                    }

                    {
                        string[] queued = null;

                        lock (_screenshotQueue)
                        {
                            queued = _screenshotQueue.ToArray();
                            _screenshotQueue.Clear();
                        }

                        // Send newly monitored file accesses to FileMonitor
                        if (queued != null && queued.Length > 0)
                        {
                            _server.ReportScreenshots(context.HostPID, queued);
                            continue;
                        }
                    }

                    {
                        _server.Ping();
                    }
                }
            }
            catch(Exception ex)
            {
                // Ping() or ReportMessages() will raise an exception if host is unreachable
                try
                {
                    lock (DarkHookLog)
                    {
                        File.AppendAllText("darkhook.log", $"{ex.ToString()}\r\n");
                    }
                }
                catch { }
            }

            // Remove hooks
            createFileHook.Dispose();
            closeHandleHook.Dispose();
            ////deleteFileHook.Dispose();
            getPrivateProfileStringHook.Dispose();
            findFirstFileExHook.Dispose();
            findNextFileWHook.Dispose();
            findCloseHook.Dispose();
            _accessHook.Dispose();
            AIL_redbook_volumeHook.Dispose();
            AIL_redbook_set_volumeeHook.Dispose();

            volSave.Flush();
            volSave.Close();
            volSave.Dispose();

            // Finalise cleanup of hooks
            EasyHook.LocalHook.Release();
        }

        [JsonConverter(typeof(StringEnumConverter))]
        enum PathType
        {
            Internal, // related to our hook engine
            NotFilePass, // Starts with \\
            HardPass,
            CachedPass,
            CachedRedirect,
            Pass,
            Redirect,
            SoftPass,
            Override, // we overrode the normal logic to do crazy stuff
            Composite, // this is a composite notice
            Screenshot, // this is a screenshot file
        }

        Regex ScreenshotFilename = new Regex(@"tact[0-9]{4}\.pcx");

        /// <summary>
        /// Get the first valid path diving our possible files
        /// </summary>
        /// <param name="filename">file path</param>
        /// <param name="type">out type of path find</param>
        /// <returns>new file path</returns>
        private string GetFirstValidPath(string filename, out PathType type)
        {
            // wierd path like a HID device
            if (filename.StartsWith(@"\\"))
            {
                type = PathType.NotFilePass;
                return filename;
            }

            // quicksolve via memo
            if (PathRedirectCache.ContainsKey(filename))
            {
                // only log if the memo is redirecting to a new file
                if (PathRedirectCache[filename] != filename)
                {
                    type = PathType.CachedRedirect;
                }
                else
                {
                    type = PathType.CachedPass;
                }
                return PathRedirectCache[filename];
            }

            bool HadBaseInfront = false;
            string originalFilename = filename;

            // this path is rooted, so lets look deeper into that
            if (Path.IsPathRooted(filename))
            {
                // even though it's rooted, it's rooted somewhere other than our game folder
                if (!filename.StartsWith(BasePath))
                {
                    type = PathType.HardPass;
                    return filename;
                }

                // we're in our game folder via a direct rooted path
                HadBaseInfront = true;

                // make the filename local, we'll re-inject the prefix if we need to
                filename = filename.Replace(BasePath, string.Empty);
            }

            bool HadOddPeriod = false;
            if (filename.StartsWith(@".\"))
            {
                HadOddPeriod = true;
                filename = filename.Substring(2);
            }

            switch (filename.ToLowerInvariant())
            {
                case "darkhook.log":
                case "newtonsoft.json.dll":
                    type = PathType.Internal;
                    return originalFilename;
            }

            if (ScreenshotFilename.IsMatch(filename))
            {
                type = PathType.Screenshot;
                return originalFilename;
            }

            // it's a save data request
            if (filename.StartsWith(@"save\"))
            {
                if (!string.IsNullOrWhiteSpace(SaveFolder))
                {
                    filename = (HadBaseInfront ? BasePath : string.Empty) + (HadOddPeriod ? @".\" : string.Empty) + @"save\" + SaveFolder + @"\" + filename.Substring(5);
                    string SaveDir = Path.GetDirectoryName(filename);
                    if (!Directory.Exists(SaveDir))
                        Directory.CreateDirectory(SaveDir);
                    type = PathType.Redirect;
                    return filename;
                }
                type = PathType.Pass;
                return originalFilename;
            }

            if (filename.StartsWith(@"dark\"))
            {
                if (ModPaths.Count > 0)
                {
                    foreach (string modpath in ModPaths)
                    {
                        string CandidatePath = (HadBaseInfront ? BasePath : string.Empty)
                                             + (HadOddPeriod ? @".\" : string.Empty)
                                             + $@"mods\{modpath}\"
                                             + filename.Substring(5);
                        if (File.Exists(CandidatePath))
                        {
                            PathRedirectCache[originalFilename] = CandidatePath;
                            type = PathType.Redirect;
                            return CandidatePath;
                        }
                    }

                    // cache the same to same map in the memo
                    PathRedirectCache[originalFilename] = originalFilename;
                    type = PathType.Pass;
                    return originalFilename;
                }
                else
                {
                    // we have no mods, use a normal load
                    PathRedirectCache[originalFilename] = originalFilename;
                    type = PathType.Pass;
                    return originalFilename;
                }
            }

            // nothing else opened it, so pass through
            type = PathType.SoftPass;
            return originalFilename;
        }

        /*#region DeleteFileW Hook
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        delegate bool DeleteFile_Delegate(String filename);



        bool DeleteFile_Hook(String filename)
        {
            try
            {
                string originalFilename = filename;
                if (!filename.StartsWith(@"\\"))
                {
                    if (Path.IsPathRooted(filename))
                    {
                        if (filename.StartsWith(BasePath))
                        {
                            string localized_filename = filename.Replace(BasePath, string.Empty);

                            // sometimes the . local path is made absolute, really odd. just make it local
                            // TODO: make sure this is safe and we don't get into a loop where something keeps trying to expand it
                            if (filename.StartsWith(BasePath + @".\dark\"))
                            {
                                TrySendMessage($"DeleteFile\tREDIRECT\t\t\t\t\"{filename}\"\t\"{localized_filename}\"");
                                filename = localized_filename;
                            }
                            else if (filename.StartsWith(@"save\"))
                            {
                                if (!string.IsNullOrWhiteSpace(SaveFolder))
                                {
                                    string filenameOld = filename;
                                    filename = filename.Replace(BasePath + @"save\", BasePath + @"save\" + SaveFolder + @"\");
                                    TrySendMessage($"DeleteFile\tREDIRECT\t\t\t\t\"{filenameOld}\"\t\"{filename}\"");
                                    return DeleteFileW(filename);
                                }
                            }
                        }
                    }
                    if (ModPaths.Count > 0)
                    {
                        // unrooted paths that start with a period are always asset loads
                        if (filename.StartsWith(@".\dark\"))
                        {
                            if (PathRedirectCache.ContainsKey(filename))
                            {
                                //TrySendMessage($"Known Local Path Encountered \"{PathRedirectCache[filename]}\"");
                                bool retVal = DeleteFileW(PathRedirectCache[filename]);
                                if (retVal) PathRedirectCache.Remove(filename);
                                return retVal;
                            }
                            else
                            {
                                foreach (string modpath in ModPaths)
                                {
                                    string newPath = filename.Replace(@".\dark\", $@".\mods\{modpath}\");
                                    if (File.Exists(newPath))
                                    {
                                        TrySendMessage($"DeleteFile\tREDIRECT\t\t\t\t\"{filename}\"\t\"{newPath}\"");
                                        return DeleteFileW(newPath);
                                    }
                                }
                            }
                        }
                    }
                }
                TrySendMessage($"DeleteFile\tPASS\t\t\t\t\"{originalFilename}\"");
                return DeleteFileW(filename);
            }
            catch (Exception ex)
            {
                Exception ex1 = ex;
                while (ex1 != null)
                {
                    File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                    ex1 = ex1.InnerException;
                }
            }
            SetLastError(ERROR_IPSEC_IKE_ERROR);
            return false;
        }
        #endregion*/




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
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr CreateFile_Delegate(String filename, UInt32 desiredAccess, UInt32 shareMode, IntPtr securityAttributes, UInt32 creationDisposition, UInt32 flagsAndAttributes, IntPtr templateFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate bool CloseHandle_Delegate(IntPtr hObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr FindFirstFileExW_Delegate(string lpFileName, IntPtr fInfoLevelId, out Kernel32.WIN32_FIND_DATAW lpFindFileData, IntPtr fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate bool FindNextFileW_Delegate(IntPtr hFindFile, out Kernel32.WIN32_FIND_DATAW lpFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate bool FindClose_Delegate(IntPtr hFindFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate uint GetPrivateProfileString_Delegate(string lpAppName, string lpKeyName, string lpDefault, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate int _access_Delegate(String filename, int mode);


        int _access_Hook(String filename, int mode)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                PathType dirType;
                string redirectedFilename = GetFirstValidPath(filename.ToLowerInvariant(), out dirType);
                //if (dirType != PathType.NotFilePass && dirType != PathType.HardPass && dirType != PathType.CachedPass && dirType != PathType.SoftPass)
                    TrySendMessage(callID, new
                    {
                        function = "_access",
                        module = "msvcrt",
                        paramaters = new { filename = filename, mode = mode },
                        notes = new { filename = redirectedFilename, dirType = dirType }
                    });
                return Msvcrt._access(redirectedFilename, mode);
            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            Kernel32.SetLastError(Msvcrt.ENOENT);
            return -1;
        }


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
        IntPtr CreateFile_Hook(String filename, UInt32 desiredAccess, UInt32 shareMode, IntPtr securityAttributes, UInt32 creationDisposition, UInt32 flagsAndAttributes, IntPtr templateFile)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                PathType dirType;
                string redirectedFilename = GetFirstValidPath(filename.ToLowerInvariant(), out dirType);
                if(dirType == PathType.Screenshot)
                {
                    TrySendMessage(callID, new
                    {
                        function = "CreateFileW",
                        module = CreateFileW_Lib,
                        paramaters = new { filename = filename, desiredAccess = desiredAccess, shareMode = shareMode, securityAttributes = securityAttributes, creationDisposition = creationDisposition, flagsAndAttributes = flagsAndAttributes, templateFile = templateFile },
                        notes = new { dirType = dirType },
                        @out = new { filename = redirectedFilename },
                    });
                    IntPtr screenshotHandle = Kernel32.CreateFileW(redirectedFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                    lock(ScreenshotFiles)
                    {
                        ScreenshotFiles[screenshotHandle] = redirectedFilename;
                    }
                    return screenshotHandle;
                }
                else
                //if(dirType != PathType.NotFilePass && dirType != PathType.HardPass && dirType != PathType.CachedPass && dirType != PathType.SoftPass)
                //if (dirType != PathType.Internal) // we end up trapped on some OSs if we try to log the creation of our log
                if (dirType != PathType.Internal && dirType != PathType.NotFilePass && dirType != PathType.HardPass) // we end up trapped on some OSs if we try to log the creation of our log
                    TrySendMessage(callID, new
                    {
                        function = "CreateFileW",
                        module = CreateFileW_Lib,
                        paramaters = new { filename = filename, desiredAccess = desiredAccess, shareMode = shareMode, securityAttributes = securityAttributes, creationDisposition = creationDisposition, flagsAndAttributes = flagsAndAttributes, templateFile = templateFile },
                        notes = new { dirType = dirType },
                        @out = new { filename = redirectedFilename },
                    });
                return Kernel32.CreateFileW(redirectedFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            Kernel32.SetLastError(Kernel32.ERROR_IPSEC_IKE_ERROR);
            return Kernel32.INVALID_HANDLE_VALUE;
        }

        bool CloseHandle_Hook(IntPtr hObject)
        {
            lock (ScreenshotFiles)
            {
                if (ScreenshotFiles.ContainsKey(hObject))
                {
                    string filename = ScreenshotFiles[hObject];
                    TryNotifyScreenshot(filename);
                    ScreenshotFiles.Remove(hObject);

                    bool retVal = Kernel32.CloseHandle(hObject);
                    Guid callID = Guid.NewGuid();
                    TrySendMessage(callID, new
                    {
                        function = "CloseHandle",
                        module = CloseHandle_Lib,
                        paramaters = new { hObject = hObject },
                        notes = new { filename = filename },
                        @return = retVal,
                    });
                    return retVal;
                }
            }
            return Kernel32.CloseHandle(hObject);
        }

        uint GetPrivateProfileString_Hook(string lpAppName, string lpKeyName, string lpDefault, IntPtr lpReturnedString, uint nSize, string filename)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                // wierd path like a HID device or just no file at all
                if (filename == null || filename.StartsWith(@"\\"))
                {
                    /*TrySendMessage(callID, new
                    {
                        function = "GetPrivateProfileStringA",
                        module = "kernel32",
                        paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = filename },
                        notes = new { dirType = PathType.NotFilePass },
                    });*/
                    return Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, filename);
                }

                // it's not a CFG
                if (!filename.EndsWith(@".cfg"))
                {
                    /*TrySendMessage(callID, new
                    {
                        function = "GetPrivateProfileStringA",
                        module = "kernel32",
                        paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = filename },
                        notes = new { dirType = PathType.HardPass },
                    });*/
                    uint retVal = Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, filename);
                    return retVal;
                }

                bool HadBaseInfront = false;
                string originalFilename = filename;

                // this path is rooted, so lets look deeper into that
                if (Path.IsPathRooted(filename))
                {
                    // even though it's rooted, it's rooted somewhere other than our game folder
                    if (!filename.StartsWith(BasePath))
                    {
                        /*TrySendMessage(callID, new
                        {
                            function = "GetPrivateProfileStringA",
                            module = "kernel32",
                            paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = filename },
                            notes = new { dirType = PathType.Pass },
                        });*/
                        return Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, filename);
                    }

                    // we're in our game folder via a direct rooted path
                    HadBaseInfront = true;

                    // make the filename local, we'll re-inject the prefix if we need to
                    filename = filename.Replace(BasePath, string.Empty);
                }

                bool HadOddPeriod = false;
                if (filename.StartsWith(@".\"))
                {
                    HadOddPeriod = true;
                    filename = filename.Substring(2);
                }

                // it's a save data request
                if (filename.StartsWith(@"save\"))
                {
                    throw new ArgumentException("GetPrivateProfileString is trying to use a cfg file in save\"");
                }

                if (filename.StartsWith(@"dark\"))
                {
                    if (ModPaths.Count > 0)
                    {
                        List<string> SearchPaths = new List<string>();
                        foreach (string modpath in ModPaths)
                        {
                            string CandidatePath = (HadBaseInfront ? BasePath : string.Empty)
                                                 + (HadOddPeriod ? @".\" : string.Empty)
                                                 + $@"mods\{modpath}\"
                                                 + filename.Substring(5);
                            SearchPaths.Add(CandidatePath);
                        }
                        SearchPaths.Add(originalFilename);

                        List<string> MultiStringReturn = new List<string>();
                        string SingleStringReturn = lpDefault;

                        ASCIIEncoding enc = new ASCIIEncoding();

                        Stack<string> SearchedPaths = new Stack<string>();
                        foreach (string CandidatePath in SearchPaths)
                        {
                            if (File.Exists(CandidatePath))
                            {
                                TrySendMessage(callID, new
                                {
                                    function = "GetPrivateProfileStringA",
                                    module = "kernel32",
                                    paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                                    notes = new { filename = CandidatePath, dirType = PathType.Redirect },
                                });

                                byte[] buff = new byte[nSize];
                                GCHandle pinnedArray = GCHandle.Alloc(buff, GCHandleType.Pinned);
                                IntPtr buffpointer = pinnedArray.AddrOfPinnedObject();
                                uint buffSize = Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, SingleStringReturn, buffpointer, nSize, CandidatePath);
                                pinnedArray.Free();
                                if (lpAppName == null || lpKeyName == null)
                                {
                                    //string TmpString = new string(buff, 0, (int)buffSize);
                                    string TmpString = enc.GetString(buff, 0, (int)buffSize);
                                    MultiStringReturn.AddRange(TmpString.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
                                }
                                else
                                {
                                    //SingleStringReturn = new string(buff, 0, (int)buffSize);
                                    string TmpSingleStringReturn = enc.GetString(buff, 0, (int)buffSize);
                                    if(TmpSingleStringReturn.Contains(@"$PARENT"))
                                    {
                                        TmpSingleStringReturn = TmpSingleStringReturn.Replace(@"$PARENT", SingleStringReturn);
                                    }
                                    SingleStringReturn = TmpSingleStringReturn;
                                }
                                break;
                            }
                            SearchedPaths.Push(CandidatePath);
                        }

                        while(SearchedPaths.Count > 0)
                        {
                            string CandidatePath = SearchedPaths.Pop() + "add";

                            if (File.Exists(CandidatePath))
                            {
                                TrySendMessage(callID, new
                                {
                                    function = "GetPrivateProfileStringA",
                                    module = "kernel32",
                                    paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                                    notes = new { filename = CandidatePath, dirType = PathType.Composite },
                                });

                                byte[] buff = new byte[nSize];
                                GCHandle pinnedArray = GCHandle.Alloc(buff, GCHandleType.Pinned);
                                IntPtr buffpointer = pinnedArray.AddrOfPinnedObject();
                                uint buffSize = Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, SingleStringReturn, buffpointer, nSize, CandidatePath);
                                pinnedArray.Free();
                                if (lpAppName == null || lpKeyName == null)
                                {
                                    //string TmpString = new string(buff, 0, (int)buffSize);
                                    string TmpString = enc.GetString(buff, 0, (int)buffSize);
                                    MultiStringReturn.AddRange(TmpString.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
                                }
                                else
                                {
                                    //SingleStringReturn = new string(buff, 0, (int)buffSize);
                                    SingleStringReturn = enc.GetString(buff, 0, (int)buffSize);
                                }
                                //break;
                            }
                        }

                        if (lpAppName == null || lpKeyName == null)
                        {
                            string retString = string.Join("\0", MultiStringReturn.Distinct().OrderBy(dr => dr)) + '\0';
                            uint length = Math.Min((uint)retString.Length, nSize - 2);
                            //System.Buffer.BlockCopy(retString.ToCharArray(), 0, lpReturnedString, 0, (int)length);
                            for (int i = 0; i < length; i++)
                            {
                                Marshal.WriteByte(lpReturnedString, i, (byte)retString[i]);
                            }
                            //lpReturnedString[length] = '\0'; // if there's a char here just overwrite it
                            Marshal.WriteByte(lpReturnedString, (int)length, 0x00); // make sure there's a nul after our last nul
                            //lpReturnedString[nSize - 2] = '\0'; // if there's a char here just overwrite it
                            Marshal.WriteByte(lpReturnedString, (int)(nSize - 2), 0x00); // also smash a double null onto the end, which would deal with truncation
                            //lpReturnedString[nSize - 1] = '\0'; // if there's a char here just overwrite it
                            Marshal.WriteByte(lpReturnedString, (int)(nSize - 1), 0x00); // also smash a double null onto the end, which would deal with truncation
                            TrySendMessage(callID, new
                            {
                                function = "GetPrivateProfileStringA",
                                module = "kernel32",
                                paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                                notes = new { dirType = PathType.Composite },
                                @return = length,
                                @out = new { lpReturnedString = retString },
                            });
                            return length;
                        }
                        else
                        {
                            uint length = Math.Min((uint)SingleStringReturn.Length, nSize - 1);
                            //System.Buffer.BlockCopy(SingleStringReturn.ToCharArray(), 0, lpReturnedString, 0, (int)length);
                            for (int i = 0; i < length; i++)
                            {
                                Marshal.WriteByte(lpReturnedString, i, (byte)SingleStringReturn[i]);
                            }
                            //lpReturnedString[length] = '\0'; // if there's a char here just overwrite it
                            Marshal.WriteByte(lpReturnedString, (int)length, 0x00);
                            TrySendMessage(callID, new
                            {
                                function = "GetPrivateProfileStringA",
                                module = "kernel32",
                                paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                                notes = new { dirType = PathType.Composite },
                                @return = length,
                                @out = new { lpReturnedString  = SingleStringReturn },
                            });
                            return length;
                        }
                    }
                    else
                    {
                        // we have no mods, use a normal load
                        //PathRedirectCache[originalFilename] = originalFilename;
                        TrySendMessage(callID, new
                        {
                            function = "GetPrivateProfileStringA",
                            module = "kernel32",
                            paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                            notes = new { dirType = PathType.Pass },
                        });
                        return Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, originalFilename);
                    }
                }

                // nothing else opened it, so pass through
                //PathRedirectCache[originalFilename] = originalFilename;
                TrySendMessage(callID, new
                {
                    function = "GetPrivateProfileStringA",
                    module = "kernel32",
                    paramaters = new { lpAppName = lpAppName, lpKeyName = lpKeyName, lpDefault = lpDefault, lpReturnedString = lpReturnedString, nSize = nSize, filename = originalFilename },
                    notes = new { dirType = PathType.Pass },
                });
                return Kernel32.GetPrivateProfileStringA(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, originalFilename);
            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            Kernel32.SetLastError(Kernel32.ERROR_FILE_NOT_FOUND);
            {
                uint length = Math.Max((uint)lpDefault.Length, nSize - 1);
                //System.Buffer.BlockCopy(lpDefault.ToCharArray(), 0, lpReturnedString, 0, (int)length);
                for (int i = 0; i < length; i++)
                {
                    Marshal.WriteByte(lpReturnedString, i, (byte)lpDefault[i]);
                }
                //lpReturnedString[length] = '\0'; // if there's a char here just overwrite it
                Marshal.WriteByte(lpReturnedString, (int)length, 0x00);
                return length;
            }
        }








        private FindFileMeta GenerateFindData(string path)
        {
            FindFileMeta meta = new FindFileMeta()
            {
                FileIndex = -1,
            };

            Dictionary<string, Kernel32.WIN32_FIND_DATAW> files = new Dictionary<string, Kernel32.WIN32_FIND_DATAW>();
            foreach (string modpath in ModPaths)
            {
                string newPath = $@"mods\{modpath}\" + path.Substring(5);

                Kernel32.WIN32_FIND_DATAW tmp;
                IntPtr Find = Kernel32.FindFirstFileExW(newPath, IntPtr.Zero, out tmp, IntPtr.Zero, IntPtr.Zero, 0);
                if (Find == Kernel32.INVALID_HANDLE_VALUE)
                {
                    continue;
                }
                {
                    string intendedPath = tmp.cFileName.ToLowerInvariant();
                    if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                }
                for(; ;)
                {
                    if (!Kernel32.FindNextFileW(Find, out tmp))
                        break;
                    string intendedPath = tmp.cFileName.ToLowerInvariant();
                    if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                }
                Kernel32.FindClose(Find);
            }
            {
                Kernel32.WIN32_FIND_DATAW tmp;
                IntPtr Find = Kernel32.FindFirstFileExW(path, IntPtr.Zero, out tmp, IntPtr.Zero, IntPtr.Zero, 0);
                if (Find != Kernel32.INVALID_HANDLE_VALUE)
                {
                    {
                        string intendedPath = tmp.cFileName.ToLowerInvariant();
                        if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                    }
                    for (; ; )
                    {
                        if (!Kernel32.FindNextFileW(Find, out tmp))
                            break;
                        string intendedPath = tmp.cFileName.ToLowerInvariant();
                        if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                    }
                    Kernel32.FindClose(Find);
                }
            }

            meta.FileMap = files.OrderBy(dr => dr.Key).Select(dr => new Tuple<string, Kernel32.WIN32_FIND_DATAW>(dr.Key, dr.Value)).ToList();

            return meta;
        }

        private Kernel32.WIN32_FIND_DATAW GetNextFile(IntPtr findPtr, out uint error, out bool success)
        {
            lock(FindFileOverides)
            {
                FindFileOverides[findPtr].FileIndex++;

                if (FindFileOverides[findPtr].FileMap.Count == 0)
                {
                    error = Kernel32.ERROR_FILE_NOT_FOUND;
                    success = false;
                    return new Kernel32.WIN32_FIND_DATAW();
                }

                if (FindFileOverides[findPtr].FileIndex >= FindFileOverides[findPtr].FileMap.Count)
                {
                    error = Kernel32.ERROR_NO_MORE_FILES;
                    success = false;
                    return new Kernel32.WIN32_FIND_DATAW();
                }

                Tuple<string, Kernel32.WIN32_FIND_DATAW> FilePath = FindFileOverides[findPtr].FileMap[FindFileOverides[findPtr].FileIndex];

                error = 0;
                success = true;
                return FilePath.Item2;
            }
        }

        IntPtr FindFirstFileExW_Hook(string filename, IntPtr fInfoLevelId, out Kernel32.WIN32_FIND_DATAW lpFindFileData, IntPtr fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                // wierd path like a HID device
                if (filename.StartsWith(@"\\"))
                {
                    IntPtr retVal = Kernel32.FindFirstFileExW(filename, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                    /*TrySendMessage(callID, new
                    {
                        function = "FindFirstFileExW",
                        module = FindFirstFileExW_Lib,
                        paramaters = new { filename = filename },
                        @return = retVal,
                        notes = new { dirType = PathType.Pass },
                        @out = new { lpFindFileData = lpFindFileData },
                    });*/
                    return retVal;
                }

                bool HadBaseInfront = false;
                string originalFilename = filename;

                // this path is rooted, so lets look deeper into that
                if (Path.IsPathRooted(filename))
                {
                    // even though it's rooted, it's rooted somewhere other than our game folder
                    if (!filename.StartsWith(BasePath))
                    {
                        IntPtr retVal = Kernel32.FindFirstFileExW(filename, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                        /*TrySendMessage(callID, new
                        {
                            function = "FindFirstFileExW",
                            module = FindFirstFileExW_Lib,
                            paramaters = new { filename = filename },
                            @return = retVal,
                            notes = new { dirType = PathType.Pass },
                            @out = new { lpFindFileData = lpFindFileData },
                        });*/
                        return retVal;
                    }

                    // we're in our game folder via a direct rooted path
                    HadBaseInfront = true;

                    // make the filename local, we'll re-inject the prefix if we need to
                    filename = filename.Replace(BasePath, string.Empty);
                }

                bool HadOddPeriod = false;
                if (filename.StartsWith(@".\"))
                {
                    HadOddPeriod = true;
                    filename = filename.Substring(2);
                }

                // it's a save data request
                if (filename.StartsWith(@"save\"))
                {
                    if (!string.IsNullOrWhiteSpace(SaveFolder))
                    {
                        filename = (HadBaseInfront ? BasePath : string.Empty) + (HadOddPeriod ? @".\" : string.Empty) + @"save\" + SaveFolder + @"\" + filename.Substring(5);
                        string SaveDir = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(SaveDir))
                            Directory.CreateDirectory(SaveDir);
                        IntPtr retVal = Kernel32.FindFirstFileExW(filename, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                        TrySendMessage(callID, new
                        {
                            function = "FindFirstFileExW",
                            module = FindFirstFileExW_Lib,
                            paramaters = new { filename = originalFilename },
                            @return = retVal,
                            notes = new { filename = filename, dirType = PathType.Redirect },
                            @out = new { lpFindFileData = lpFindFileData },
                        });
                        return retVal;
                    }
                    {
                        IntPtr retVal = Kernel32.FindFirstFileExW(originalFilename, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                        TrySendMessage(callID, new
                        {
                            function = "FindFirstFileExW",
                            module = FindFirstFileExW_Lib,
                            paramaters = new { filename = originalFilename },
                            @return = retVal,
                            notes = new { dirType = PathType.Pass },
                            @out = new { lpFindFileData = lpFindFileData },
                        });
                        return retVal;
                    }
                }

                if (filename.StartsWith(@"dark\"))
                {
                    if (ModPaths.Count > 0)
                    {
                        object dummy = new object();
                        GCHandle retValX = GCHandle.Alloc(dummy);

                        lock (FindFileOverides)
                        {
                            FindFileOverides[(IntPtr)retValX] = GenerateFindData(filename.ToLowerInvariant());

                            uint error = 0;
                            bool success = false;
                            lpFindFileData = GetNextFile((IntPtr)retValX, out error, out success);
                            Kernel32.SetLastError(error);

                            if (!success)
                            {
                                FindFileOverides.Remove((IntPtr)retValX);
                                retValX.Free();
                                TrySendMessage(callID, new
                                {
                                    function = "FindFirstFileExW",
                                    module = FindFirstFileExW_Lib,
                                    paramaters = new { filename = originalFilename },
                                    @return = Kernel32.INVALID_HANDLE_VALUE,
                                    notes = new { success = success, error = error, dirType = PathType.Override },
                                    @out = new { lpFindFileData = lpFindFileData },
                                });
                                return Kernel32.INVALID_HANDLE_VALUE;
                            }

                            TrySendMessage(callID, new
                            {
                                function = "FindFirstFileExW",
                                module = FindFirstFileExW_Lib,
                                paramaters = new { filename = originalFilename },
                                @return = (IntPtr)retValX,
                                notes = new { success = success, error = error, dirType = PathType.Override },
                                @out = new { lpFindFileData = lpFindFileData },
                            });
                            return (IntPtr)retValX;
                        }
                    }
                    {
                        // we have no mods, use a normal scan
                        IntPtr retVal = Kernel32.FindFirstFileExW(originalFilename, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                        int error = Marshal.GetLastWin32Error();
                        TrySendMessage(callID, new
                        {
                            function = "FindFirstFileExW",
                            module = FindFirstFileExW_Lib,
                            paramaters = new { filename = originalFilename },
                            @return = retVal,
                            notes = new { error = error, dirType = PathType.Pass },
                            @out = new { lpFindFileData = lpFindFileData },
                        });
                        return retVal;
                    }
                }
                {
                    IntPtr retVal = Kernel32.FindFirstFileExW(originalFilename,  fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
                    int error = Marshal.GetLastWin32Error();
                    TrySendMessage(callID, new
                    {
                        function = "FindFirstFileExW",
                        module = FindFirstFileExW_Lib,
                        paramaters = new { filename = originalFilename },
                        @return = retVal,
                        notes = new { error = error, dirType = PathType.Pass },
                        @out = new { lpFindFileData = lpFindFileData },
                    });
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            lpFindFileData = new Kernel32.WIN32_FIND_DATAW();
            Kernel32.SetLastError(Kernel32.ERROR_IPSEC_IKE_ERROR);
            return Kernel32.INVALID_HANDLE_VALUE;
        }

        bool FindNextFileW_Hook(IntPtr hFindFile, out Kernel32.WIN32_FIND_DATAW lpFindFileData)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                lock (FindFileOverides)
                {
                    if (FindFileOverides.ContainsKey(hFindFile))
                    {
                        uint error = 0;
                        bool success = false;
                        lpFindFileData = GetNextFile(hFindFile, out error, out success);
                        Kernel32.SetLastError(error);
                        TrySendMessage(callID, new
                        {
                            function = "FindNextFileW",
                            module = FindNextFileW_Lib,
                            paramaters = new { hFindFile = hFindFile },
                            @return = success,
                            notes = new {
                                success = success,
                                error = error,
                                dirType = PathType.Override,
                            },
                            @out = new { lpFindFileData = lpFindFileData },
                        });
                        return success;
                    }
                }
                /*TrySendMessage(callID, new
                {
                    function = "FindNextFileW",
                    module = FindNextFileW_Lib,
                    paramaters = new { hFindFile = hFindFile },
                    @return = VolSet,
                    notes = new { dirType = PathType.Pass },
                });*/
                return Kernel32.FindNextFileW(hFindFile, out lpFindFileData);
            }
            catch (Exception ex) 
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            lpFindFileData = new Kernel32.WIN32_FIND_DATAW();
            Kernel32.SetLastError(Kernel32.ERROR_IPSEC_IKE_ERROR);
            return false;
        }

        bool FindClose_Hook(IntPtr hFindFile)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                lock (FindFileOverides)
                {
                    if (FindFileOverides.ContainsKey(hFindFile))
                    {
                        FindFileOverides.Remove(hFindFile);
                        ((GCHandle)hFindFile).Free();
                        TrySendMessage(callID, new
                        {
                            function = "FindClose",
                            module = FindClose_Lib,
                            paramaters = new { hFindFile = hFindFile },
                            @return = VolSet,
                            notes = new { dirType = PathType.Override },
                        });
                        return true;
                    }
                }
                /*TrySendMessage(callID, new
                {
                    function = "FindClose",
                    module = FindClose_Lib,
                    paramaters = new { hFindFile = hFindFile },
                    notes = new { dirType = PathType.Pass },
                });*/
                return Kernel32.FindClose(hFindFile);
            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            Kernel32.SetLastError(Kernel32.ERROR_IPSEC_IKE_ERROR);
            return false;
        }










        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate Int32 AIL_redbook_volume_Delegate(ref Winmm.REDBOOK hand);



        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate Int32 AIL_redbook_set_volume_Delegate(ref Winmm.REDBOOK hand, Int32 volume);

        Int32 AIL_redbook_volume_Hook(ref Winmm.REDBOOK hand)
        {
            Guid callID = Guid.NewGuid();

            TrySendMessage(callID, new
            {
                function = "_AIL_redbook_volume",
                module = "mss32",
                paramaters = new { hand = hand },
                @return = VolSet,
            });
            return VolSet;
        }

        /// <summary>
        /// Hook for redbook volume set in Miles Sound System (mss32.dll)
        /// </summary>
        /// <param name="hand">REDBOOK struct</param>
        /// <param name="volume">0-127, in practice the game sends 0-125</param>
        /// <returns></returns>
        Int32 AIL_redbook_set_volume_Hook(ref Winmm.REDBOOK hand, Int32 volume)
        {
            Guid callID = Guid.NewGuid();

            try
            {
                uint newVol = Math.Min((uint)(volume * (65535.0f / 124)), 0xFFFF);
                Winmm.auxSetVolume((int)hand.DeviceID, newVol);
                VolLast = VolSet;
                VolSet = (byte)volume;
                TrySendMessage(callID, new
                {
                    function = "_AIL_redbook_set_volume",
                    module = "mss32",
                    paramaters = new { hand = hand, volume = volume },
                    @return = volume,
                    notes = new { sentVolume = $"0x{newVol:X4}", prevVolume = VolLast },
                });

                volSave.SetLength(0);
                volSave.WriteByte(VolLast);
                volSave.WriteByte(VolSet);

            }
            catch (Exception ex)
            {
                lock (DarkHookLog)
                {
                    Exception ex1 = ex;
                    while (ex1 != null)
                    {
                        File.AppendAllText("darkhook.log", "EXCEPTION " + DateTime.Now + "\r\n" + ex1.ToString() + "\r\n");
                        ex1 = ex1.InnerException;
                    }
                }
            }
            return volume;
        }


        private void TryNotifyScreenshot(string filename)
        {
            lock (this._screenshotQueue)
            {
                if (this._screenshotQueue.Count < 1000)
                {
                    this._screenshotQueue.Enqueue(filename);
                }
            }
        }

        private void TrySendMessage(Guid id, dynamic obj)
        {
            string message = id.ToString() + "\tDESERIALIZE ERROR";

            try
            {
                message = id.ToString() + "\t" + JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None);

                /*lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        this._messageQueue.Enqueue(message);
                    }
                }*/
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }
            try
            {
                lock (DarkHookLog)
                {
                    File.AppendAllText("darkhook.log", $"MESSAGE\t{message}\r\n");
                }
            }
            catch { }
        }

        public class FindFileMeta
        {
            public int FileIndex { get; set; }

            public List<Tuple<string, Kernel32.WIN32_FIND_DATAW>> FileMap { get; set; }
        }
    }
}
