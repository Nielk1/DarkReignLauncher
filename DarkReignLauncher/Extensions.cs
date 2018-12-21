using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkReignLauncher
{
    public static class Extensions
    {
        public static void Suspend(this Process process)
        {
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = Kernel32.OpenThread(Kernel32.ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                Kernel32.SuspendThread(pOpenThread);

                Kernel32.CloseHandle(pOpenThread);
            }
        }

        public static void Resume(this Process process)
        {
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = Kernel32.OpenThread(Kernel32.ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = Kernel32.ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                Kernel32.CloseHandle(pOpenThread);
            }
        }

        public static IEnumerable<IntPtr> EnumerateWindowHandles(this Process process)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in process.Threads)
            {
                User32.EnumThreadWindows(
                    thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; },
                    IntPtr.Zero);
            }

            return handles;
        }
    }
}
