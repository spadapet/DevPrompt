using System;
using System.Diagnostics;
using System.Threading;

namespace DevPrompt
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (ManualResetEvent exitEvent = new ManualResetEvent(false))
            {
                Thread mainProcessThread = new Thread(() => Program.WaitToBecomeMainProcess(exitEvent));
                mainProcessThread.Start();

                try
                {
                    App.Main();
                }
                finally
                {
                    exitEvent.Set();
                }

                mainProcessThread.Join();
            }
        }

        private static void WaitToBecomeMainProcess(EventWaitHandle exitEvent)
        {
            using (Mutex mainProcessMutex = new Mutex(false, "{5553691f-212c-4535-a82a-586b11fbd1bb}", out bool firstInstance))
            {
                try
                {
                    if (WaitHandle.WaitAny(new WaitHandle[] { mainProcessMutex, exitEvent }) != 0)
                    {
                        return;
                    }
                }
                catch (AbandonedMutexException ex)
                {
                    Debug.Assert(ex.MutexIndex == 0 && ex.Mutex == mainProcessMutex);
                }

                Program.IsMainProcess = true;

                try
                {
                    exitEvent.WaitOne();
                }
                finally
                {
                    Program.IsMainProcess = false;
                    mainProcessMutex.ReleaseMutex();
                }
            }
        }

        public static bool IsMicrosoftDomain { get; }
        public static bool IsNotMicrosoftDomain => !Program.IsMicrosoftDomain;
        private static long isMainProcessCount;

        public static bool IsMainProcess
        {
            get
            {
                return Interlocked.Read(ref Program.isMainProcessCount) != 0;
            }

            private set
            {
                Interlocked.Exchange(ref Program.isMainProcessCount, value ? 1 : 0);
            }
        }

        static Program()
        {
            Program.IsMicrosoftDomain = string.Equals(
                Environment.GetEnvironmentVariable("USERDNSDOMAIN"),
                "REDMOND.CORP.MICROSOFT.COM",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
