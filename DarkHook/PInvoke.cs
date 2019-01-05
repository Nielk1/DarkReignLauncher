using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DarkHook
{
    public class Kernel32
    {
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public const uint ERROR_IPSEC_IKE_ERROR = 0x000035F8;

        public const int ERROR_FILE_NOT_FOUND = 0x02;
        public const int ERROR_NO_MORE_FILES = 0x12;

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string filename);

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
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateFileW(String filename, UInt32 desiredAccess, UInt32 shareMode, IntPtr securityAttributes, UInt32 creationDisposition, UInt32 flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern uint GetPrivateProfileStringA(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr FindFirstFileA(string lpFileName, out WIN32_FIND_DATAA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool FindNextFileA(IntPtr hFindFile, out WIN32_FIND_DATAA lpFindFileData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WIN32_FIND_DATAA
        {
            public int dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
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


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        /// <summary>
        /// Sets the last-error code for the calling thread.
        /// </summary>
        /// <param name="dwErrorCode">The last-error code for the thread.</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void SetLastError(uint dwErrorCode);
    }

    public class Winmm
    {
        //[DllImport("mss32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint= "_AIL_redbook_volume@4", SetLastError = false)]
        //public static extern Int32 _AIL_redbook_volume(ref REDBOOK hand);

        //[DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi, SetLastError = false)]
        //public static extern uint auxGetVolume(int uDeviceID, ref uint volume);

        //[DllImport("mss32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_AIL_redbook_set_volume@8", SetLastError = false)]
        //public static extern Int32 _AIL_redbook_set_volume(ref REDBOOK hand, Int32 volume);

        [DllImport("winmm.dll", CallingConvention = CallingConvention.Winapi, SetLastError = false)]
        public static extern uint auxSetVolume(int uDeviceID, uint dwVolume);

        [StructLayout(LayoutKind.Sequential)]
        public struct REDBOOK
        {
            public UInt32 DeviceID;
            public UInt32 paused;
            public UInt32 pausedsec;
            public UInt32 lastendsec;
        }
    }
}
