using System;
using System.Diagnostics;

namespace DevPrompt.Utility
{
    internal static class TimerUtility
    {
        public static TimeSpan GetElapsedTimeAndRestart(this Stopwatch timer)
        {
            TimeSpan time = timer.Elapsed;
            timer.Restart();
            return time;
        }

        public static TimeSpan GetElapsedTimeAndStop(this Stopwatch timer)
        {
            TimeSpan time = timer.Elapsed;
            timer.Stop();
            return time;
        }
    }
}
