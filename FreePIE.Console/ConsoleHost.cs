using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using FreePIE.Core.Common;
using FreePIE.Core.Common.Events;
using FreePIE.Core.Model.Events;
using FreePIE.Core.Persistence;
using FreePIE.Core.ScriptEngine;

namespace FreePIE.Console
{
    public class ConsoleHost : IHandle<WatchEvent>, IHandle<ScriptErrorEvent>
    {
        #region Trap application termination
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private bool Handler(CtrlType sig)
        {
            Stop();

            return true;
        }
        #endregion

        private readonly IScriptEngine scriptEngine;
        private readonly IPersistanceManager persistanceManager;
        private readonly IFileSystem fileSystem;
        private readonly AutoResetEvent waitUntilStopped;
        private int stopped;

        public ConsoleHost(IScriptEngine scriptEngine, IPersistanceManager persistanceManager, IFileSystem fileSystem, IEventAggregator eventAggregator)
        {
            // Some boilerplate to react to close window event, CTRL-C, kill, etc
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            this.scriptEngine = scriptEngine;
            this.persistanceManager = persistanceManager;
            this.fileSystem = fileSystem;
            waitUntilStopped = new AutoResetEvent(false);

            eventAggregator.Subscribe(this);
        }

        public void Start(string[] args)
        {

            try
            {
                string script = null;


                if (args.Length == 0) {
                    PrintHelp();
                    return;
                }

                try
                {
                    script = fileSystem.ReadAllText(args[0]);
                }
                catch (IOException)
                {
                    System.Console.WriteLine("Can't open script file");
                    throw;
                }

                string profile = null;
                if (args.Length > 1)
                {
                    profile = args[1];
                }

                System.Console.TreatControlCAsInput = false;
                System.Console.CancelKeyPress += (s, e) => Stop();

                persistanceManager.Load();

                System.Console.WriteLine("Starting script parser");

                scriptEngine.Start(script, args[0], profile);
                waitUntilStopped.WaitOne();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        private void Stop()
        {
            int stop = Interlocked.Exchange(ref stopped, 1);
            if (stop == 1)
                return;

            System.Console.WriteLine("Stopping script parser");
            scriptEngine.Stop();

            persistanceManager.Save();
            waitUntilStopped.Set();
        }

        public void Handle(WatchEvent message)
        {
            System.Console.WriteLine("{0}: {1}", message.Name, message.Value);
        }

        public void Handle(ScriptErrorEvent message)
        {
            System.Console.WriteLine(message.Description);
            if(message.Level == ErrorLevel.Exception)
                Stop();
        }

        private void PrintHelp()
        {
            System.Console.WriteLine("FreePIE.Console.exe <script_file> <optional_profile>");
        }
    }
}
