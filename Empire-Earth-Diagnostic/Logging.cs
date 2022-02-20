using System;
using System.Diagnostics;
using System.IO;

namespace Empire_Earth_Diagnostic
{
    class Logging
    {

        public enum LogLevel
        {
            Info, Warning, Error
        }

        /// <summary>
        /// Launcher logging, this will redirect the console to log file
        /// <br>Don't call it multiple time or previous the one will not work !</br>
        /// </summary>
        public Logging()
        {
            Trace.Listeners.Clear();

            // When > 1 Mo => Clean and keep 500 lines
            if (File.Exists("log.txt"))
            {
                if (new FileInfo("log.txt").Length > 1048576 /* 1 Mo */)
                {
                    string[] lines = File.ReadAllLines("log.txt");
                    File.Delete("log.txt");
                    for (int i = (lines.Length - 500); i != lines.Length;  ++i)
                    {
                        Console.WriteLine(lines[i]);
                        File.AppendAllText("log.txt", lines[i] + "\n");
                    }
                }
            }

            TextWriterTraceListener twtl = new TextWriterTraceListener("log.txt");
            twtl.Name = "Empire Earth Diagnostic Logger";

            ConsoleTraceListener ctl = new ConsoleTraceListener(false);
            ctl.TraceOutputOptions = TraceOptions.DateTime;

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;
        }

        public void Log(string log, LogLevel level)
        {
            Trace.WriteLine("[" + DateTime.Now + "] " + level + " : " + log);
        }

        public void Log(string log)
        {
            Log(log, LogLevel.Info);
        }

        public void Log(string log, Exception ex)
        {
            Log(log + "\n" + ex.Message + "\n" + ex.StackTrace, LogLevel.Error);
        }
    }
}
