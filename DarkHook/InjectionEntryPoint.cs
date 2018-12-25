﻿using System;
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
        string SaveFolder;

        Dictionary<string, string> PathRedirectCache;
        Dictionary<IntPtr, FindFileMeta> FindFileOverides;

        FileStream volSave;
        byte VolSet = 127;
        byte VolLast = 127;

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
            List<string> ModPaths,
            string SaveFolder)
        {
            // Injection is now complete and the server interface is connected
            _server.IsInstalled(EasyHook.RemoteHooking.GetCurrentProcessId());

            // Install hooks

            // CreateFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
            var createFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"), new CreateFile_Delegate(CreateFile_Hook), this);
            //var deleteFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "DeleteFileW"), new DeleteFile_Delegate(DeleteFile_Hook), this);
            var findFirstFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileA"), new FindFirstFile_Delegate(FindFirstFile_Hook), this);
            var findNextFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "FindNextFileA"), new FindNextFile_Delegate(FindNextFile_Hook), this);
            var findCloseHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "FindClose"), new FindClose_Delegate(FindClose_Hook), this);
            var AIL_redbook_volumeHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("mss32.dll", "_AIL_redbook_volume@4"), new AIL_redbook_volume_Delegate(AIL_redbook_volume_Hook), this);
            var AIL_redbook_set_volumeeHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("mss32.dll", "_AIL_redbook_set_volume@8"), new AIL_redbook_set_volume_Delegate(AIL_redbook_set_volume_Hook), this);

            /*try
            {
                var AIL_redbook_set_volumeeHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("mss32.dll", "_AIL_redbook_set_volume@8"), new AIL_redbook_set_volume_Delegate(AIL_redbook_set_volume_Hook), this);
            }
            catch (Exception ex)
            {
                Exception ex1 = ex;
                while (ex1 != null)
                {
                    File.AppendAllText("~hooklog.txt", ex1.ToString() + "\r\n\r\n\r\n");
                    ex1 = ex1.InnerException;
                }
            }*/

            File.WriteAllText("darkhook.log", "DarkHook Log " + DateTime.Now + "\r\n");

            // Activate hooks on all threads except the current thread
            createFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            //deleteFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findNextFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findCloseHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            AIL_redbook_volumeHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            AIL_redbook_set_volumeeHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            _server.ReportMessage(context.HostPID, "CreateFileW, DeleteFileW, FindFirstFileA, FindNextFileA, FindCloseA hooks installed");

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
            //deleteFileHook.Dispose();
            findFirstFileHook.Dispose();
            findNextFileHook.Dispose();
            findCloseHook.Dispose();
            AIL_redbook_volumeHook.Dispose();
            AIL_redbook_set_volumeeHook.Dispose();

            volSave.Flush();
            volSave.Close();
            volSave.Dispose();

            // Finalise cleanup of hooks
            EasyHook.LocalHook.Release();
        }

        #region DeleteFileW Hook
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        delegate bool DeleteFile_Delegate(String filename);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string filename);

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
        #endregion

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
            try
            {
                // wierd path like a HID device
                if (filename.StartsWith(@"\\"))
                {
                    //TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{filename}\"");
                    return CreateFileW(filename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                }

                // quicksolve via memo
                if (PathRedirectCache.ContainsKey(filename))
                {
                    // only log if the memo is redirecting to a new file
                    if (PathRedirectCache[filename] != filename)
                    {
                        TrySendMessage($"CreateFile\tREDIRECT[MEMO]\t\t\t\t\"{filename}\"\t\"{PathRedirectCache[filename]}\"");
                    }
                    else
                    {
                        //TrySendMessage($"CreateFile\tPASS[MEMO]\t\t\t\t\"{filename}\"");
                    }
                    return CreateFileW(PathRedirectCache[filename], desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                }

                bool HadBaseInfront = false;
                string originalFilename = filename;

                // this path is rooted, so lets look deeper into that
                if (Path.IsPathRooted(filename))
                {
                    // even though it's rooted, it's rooted somewhere other than our game folder
                    if (!filename.StartsWith(BasePath))
                    {
                        //TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{filename}\"");
                        return CreateFileW(filename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
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
                        TrySendMessage($"CreateFile\tREDIRECT\t\t\t\t\"{originalFilename}\"\t\"{filename}\"");
                        return CreateFileW(filename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                    }
                    TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{originalFilename}\"");
                    return CreateFileW(originalFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
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
                                TrySendMessage($"CreateFile\tREDIRECT\t\t\t\t\"{originalFilename}\"\t\"{CandidatePath}\"");
                                return CreateFileW(CandidatePath, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                            }
                        }

                        // cache the same to same map in the memo
                        PathRedirectCache[originalFilename] = originalFilename;
                        TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{originalFilename}\"");
                        return CreateFileW(originalFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                    }
                    else
                    {
                        // we have no mods, use a normal load
                        PathRedirectCache[originalFilename] = originalFilename;
                        TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{originalFilename}\"");
                        return CreateFileW(originalFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
                    }
                }

                // nothing else opened it, so pass through
                PathRedirectCache[originalFilename] = originalFilename;
                //TrySendMessage($"CreateFile\tPASS\t\t\t\t\"{originalFilename}\"");
                return CreateFileW(originalFilename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
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
            return INVALID_HANDLE_VALUE;
        }
        #endregion



        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr FindFirstFileA(string lpFileName, out WIN32_FIND_DATAA lpFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr FindFirstFile_Delegate(string lpFileName, out WIN32_FIND_DATAA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool FindNextFileA(IntPtr hFindFile, out WIN32_FIND_DATAA lpFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate bool FindNextFile_Delegate(IntPtr hFindFile, out WIN32_FIND_DATAA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate bool FindClose_Delegate(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WIN32_FIND_DATAA
        {
            public int dwFileAttributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
            /*public int dwFileType;
            public int dwCreatorType;
            public Int16 wFinderFlags;*/
        }

        /// <summary>
        /// Sets the last-error code for the calling thread.
        /// </summary>
        /// <param name="dwErrorCode">The last-error code for the thread.</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetLastError(uint dwErrorCode);

        IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        const uint ERROR_IPSEC_IKE_ERROR = 0x000035F8;

        private FindFileMeta GenerateFindData(string path)
        {
            FindFileMeta meta = new FindFileMeta()
            {
                FileIndex = -1,
            };

            Dictionary<string, WIN32_FIND_DATAA> files = new Dictionary<string, WIN32_FIND_DATAA>();
            foreach (string modpath in ModPaths)
            {
                string newPath = $@"mods\{modpath}\" + path.Substring(5);

                WIN32_FIND_DATAA tmp;
                IntPtr Find = FindFirstFileA(newPath, out tmp);
                if (Find == INVALID_HANDLE_VALUE)
                {
                    continue;
                }
                {
                    string intendedPath = tmp.cFileName;
                    if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                }
                for(; ;)
                {
                    if (!FindNextFileA(Find, out tmp))
                        break;
                    string intendedPath = tmp.cFileName;
                    if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                }
                FindClose(Find);
            }
            {
                WIN32_FIND_DATAA tmp;
                IntPtr Find = FindFirstFileA(path, out tmp);
                if (Find != INVALID_HANDLE_VALUE)
                {
                    {
                        string intendedPath = tmp.cFileName;
                        if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                    }
                    for (; ; )
                    {
                        if (!FindNextFileA(Find, out tmp))
                            break;
                        string intendedPath = tmp.cFileName;
                        if (!files.ContainsKey(intendedPath)) files[intendedPath] = tmp;
                    }
                    FindClose(Find);
                }
            }

            meta.FileMap = files.OrderBy(dr => dr.Key).Select(dr => new Tuple<string, WIN32_FIND_DATAA>(dr.Key, dr.Value)).ToList();

            return meta;
        }

        const int ERROR_FILE_NOT_FOUND = 0x02;
        const int ERROR_NO_MORE_FILES = 0x12;
        private WIN32_FIND_DATAA GetNextFile(IntPtr findPtr, out uint error, out bool success)
        {
            lock(FindFileOverides)
            {
                FindFileOverides[findPtr].FileIndex++;

                if (FindFileOverides[findPtr].FileMap.Count == 0)
                {
                    error = ERROR_FILE_NOT_FOUND;
                    success = false;
                    return new WIN32_FIND_DATAA();
                }

                if (FindFileOverides[findPtr].FileIndex >= FindFileOverides[findPtr].FileMap.Count)
                {
                    error = ERROR_NO_MORE_FILES;
                    success = false;
                    return new WIN32_FIND_DATAA();
                }

                Tuple<string, WIN32_FIND_DATAA> FilePath = FindFileOverides[findPtr].FileMap[FindFileOverides[findPtr].FileIndex];

                error = 0;
                success = true;
                return FilePath.Item2;
            }
        }

        IntPtr FindFirstFile_Hook(string filename, out WIN32_FIND_DATAA lpFindFileData)
        {
            try {
                // wierd path like a HID device
                if (filename.StartsWith(@"\\"))
                {
                    IntPtr retVal = FindFirstFileA(filename, out lpFindFileData);
                    //TrySendMessage($"FindFirstFile\tPASS\t{retVal}\t\t\t\"{filename}\"");
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
                        IntPtr retVal = FindFirstFileA(filename, out lpFindFileData);
                        //TrySendMessage($"FindFirstFile\tPASS\t{retVal}\t\t\t\"{filename}\"");
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
                        IntPtr retVal = FindFirstFileA(filename, out lpFindFileData);
                        TrySendMessage($"FindFirstFile\tREDIRECT\t{retVal}\t\t\t\"{originalFilename}\"\t\"{filename}\"");
                        return retVal;
                    }
                    {
                        IntPtr retVal = FindFirstFileA(originalFilename, out lpFindFileData);
                        TrySendMessage($"FindFirstFile\tPASS\t{retVal}\t\t\t\"{originalFilename}\"");
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
                            FindFileOverides[(IntPtr)retValX] = GenerateFindData(filename);

                            uint error = 0;
                            bool success = false;
                            lpFindFileData = GetNextFile((IntPtr)retValX, out error, out success);
                            SetLastError(error);

                            if (!success)
                            {
                                FindFileOverides.Remove((IntPtr)retValX);
                                retValX.Free();
                                TrySendMessage($"FindFirstFile\tOVERRIDE\t-1\tsuccess:{success}\terror:{error}\t\"{originalFilename}\"");
                                return INVALID_HANDLE_VALUE;
                            }

                            TrySendMessage($"FindFirstFile\tOVERRIDE\t{(IntPtr)retValX}\tsuccess:{success}\terror:{error}\t\"{originalFilename}\"\t\"{lpFindFileData.cFileName}\"");
                            return (IntPtr)retValX;
                        }
                    }
                    {
                        // we have no mods, use a normal scan
                        IntPtr retVal = FindFirstFileA(originalFilename, out lpFindFileData);
                        int error = Marshal.GetLastWin32Error();
                        //TrySendMessage($"FindFirstFile\tPASS\t{retVal}\t\terror:{error}\t\"{originalFilename}\"");
                        return retVal;
                    }
                }
                {
                    IntPtr retVal = FindFirstFileA(originalFilename, out lpFindFileData);
                    int error = Marshal.GetLastWin32Error();
                    //TrySendMessage($"FindFirstFile\tPASS\t{retVal}\t\terror:{error}\t\"{originalFilename}\"");
                    return retVal;
                }
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
            lpFindFileData = new WIN32_FIND_DATAA();
            SetLastError(ERROR_IPSEC_IKE_ERROR);
            return INVALID_HANDLE_VALUE;
        }

        bool FindNextFile_Hook(IntPtr hFindFile, out WIN32_FIND_DATAA lpFindFileData)
        {
            try {
                lock (FindFileOverides)
                {
                    if (FindFileOverides.ContainsKey(hFindFile))
                    {
                        uint error = 0;
                        bool success = false;
                        lpFindFileData = GetNextFile(hFindFile, out error, out success);
                        SetLastError(error);
                        TrySendMessage($"FindNextFile\tOVERRIDE\t{hFindFile}\tsuccess:{success}\terror:{error}\t\"{lpFindFileData.cFileName}\"");
                        return success;
                    }
                }
                //TrySendMessage($"FindNextFile\tPASS\t{hFindFile}");
                return FindNextFileA(hFindFile, out lpFindFileData);
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
            lpFindFileData = new WIN32_FIND_DATAA();
            SetLastError(ERROR_IPSEC_IKE_ERROR);
            return false;
        }

        bool FindClose_Hook(IntPtr hFindFile)
        {
            try {
                lock (FindFileOverides)
                {
                    if (FindFileOverides.ContainsKey(hFindFile))
                    {
                        FindFileOverides.Remove(hFindFile);
                        ((GCHandle)hFindFile).Free();
                        TrySendMessage($"FindClose\tOVERRIDE\t{hFindFile}");
                        return true;
                    }
                }
                //TrySendMessage($"FindClose\tPASS\t{hFindFile}");
                return FindClose(hFindFile);
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




        [StructLayout(LayoutKind.Sequential)]
        public struct REDBOOK
        {
            public UInt32 DeviceID;
            public UInt32 paused;
            public UInt32 pausedsec;
            public UInt32 lastendsec;
        }

        //[DllImport("mss32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint= "_AIL_redbook_volume@4", SetLastError = false)]
        //public static extern Int32 _AIL_redbook_volume(ref REDBOOK hand);

        //[DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi, SetLastError = false)]
        //public static extern uint auxGetVolume(int uDeviceID, ref uint volume);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate Int32 AIL_redbook_volume_Delegate(ref REDBOOK hand);

        //[DllImport("mss32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_AIL_redbook_set_volume@8", SetLastError = false)]
        //public static extern Int32 _AIL_redbook_set_volume(ref REDBOOK hand, Int32 volume);

        [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi, SetLastError = false)]
        public static extern uint auxSetVolume(int uDeviceID, uint dwVolume);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate Int32 AIL_redbook_set_volume_Delegate(ref REDBOOK hand, Int32 volume);

        Int32 AIL_redbook_volume_Hook(ref REDBOOK hand)
        {
            //uint vol = 0;
            //uint retVal = auxGetVolume((int)hand.DeviceID, ref vol);
            //TrySendMessage($"AIL_redbook_volume\t{hand.DeviceID:X}\t{VolSet}");
            //return (int)(vol / (65535.0f / 124));
            return VolSet;
        }

        Int32 AIL_redbook_set_volume_Hook(ref REDBOOK hand, Int32 volume)
        {
            try
            {
                uint newVol = Math.Min((uint)(volume * (65535.0f / 124)), 0xFFFF);
                auxSetVolume((int)hand.DeviceID, newVol);
                VolLast = VolSet;
                VolSet = (byte)volume;
                //TrySendMessage($"AIL_redbook_set_volume\t{hand.DeviceID:X}\t{VolSet}\t{newVol:X}");

                volSave.SetLength(0);
                volSave.WriteByte(VolLast);
                volSave.WriteByte(VolSet);

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
            return volume;
        }




        private void TrySendMessage(string message)
        {
            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        this._messageQueue.Enqueue(message);
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }
            try
            {
                File.AppendAllText("darkhook.log", "MESSAGE " + message + "\r\n");
            }
            catch { }
        }

        public class FindFileMeta
        {
            public int FileIndex { get; set; }

            public List<Tuple<string, WIN32_FIND_DATAA>> FileMap { get; set; }
        }
    }
}
