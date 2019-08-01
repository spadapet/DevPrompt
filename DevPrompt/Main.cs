using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;

namespace DevPrompt
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (Program.HandleArguments(args))
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
        }

        private static bool HandleArguments(string[] args)
        {
            const string adminSwitch = "/admin";
            const string waitForSwitch = "/waitfor";

            // Check if we have to wait for a parent process to die
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == waitForSwitch && int.TryParse(args[i + 1], out int waitForProcessId))
                {
                    string name = Process.GetCurrentProcess().ProcessName;
                    if (Process.GetProcessesByName(name).FirstOrDefault(p => p.Id == waitForProcessId) is Process waitForProcess)
                    {
                        waitForProcess.WaitForExit();
                    }
                }
            }

            // Check if this process should restart as elevated
            if (args.Length > 0 && args[0] == adminSwitch && !Program.IsElevated)
            {
                // Try to run again as admin
                int adminIndex = Environment.CommandLine.IndexOf(adminSwitch, StringComparison.Ordinal);
                if (adminIndex >= 0)
                {
                    string newExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string newArgs = Environment.CommandLine.Remove(0, adminIndex + adminSwitch.Length).Trim();

                    ProcessStartInfo info = new ProcessStartInfo(newExe, newArgs)
                    {
                        UseShellExecute = true
                    };

                    if (info.Verbs.Contains("runas"))
                    {
                        info.Verb = "runas";
                        try
                        {
                            if (Process.Start(info) is Process process)
                            {
                                // Quit and let the new process take over
                                return false;
                            }
                        }
                        catch
                        {
                            // Just keep running this process
                        }
                    }
                }
            }

            // No need to restart
            return true;
        }

        /// <summary>
        /// When there are multiple processes running, only one can be the "Main" process.
        /// The main process is the one that is allowed to auto-grab consoles.
        /// This method will wait until either:
        /// - It's time to become the main process (the other main process exited)
        /// - It's time to exit the program
        /// </summary>
        private static void WaitToBecomeMainProcess(EventWaitHandle exitEvent)
        {
            using (Mutex mainProcessMutex = new Mutex(false, "{5553691f-212c-4535-a82a-586b11fbd1bb}", out _))
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
        public static bool IsElevated { get; }
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

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            Program.IsElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
