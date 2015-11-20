using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public struct ActionData
    {
        public string inMessage;
        public string outMessage;
    }

    public enum actionManager
    {
        OPEN,
        UPDATE,
        CLOSE,
        LOGOUT,
        TAKEPICTURE
    }

    public class ActionManager : IObservable<ActionData>
    {
        RSSManager rssManager;

        public static void ProcessCommand(String command, object commandObject)
        {
            if (commandObject.Equals(actionManager.LOGOUT))
            {
                CommandLogout();
            }
        }

        public ActionManager()
        {
            rssManager = new RSSManager();
        }

        public IDisposable Subscribe(IObserver<ActionData> observer)
        {
            return null;
        }

        public static void CommandLogout()
        {
            System.Diagnostics.Process.Start("shutdown", "-l");
        }

        public static void CommandOpenApplication(String app)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = app;
            proc.Start();
        }

        public static void CommandCloseApplication(String app)
        {
            System.Diagnostics.Process[] procs = null;
            try
            {
                procs = System.Diagnostics.Process.GetProcessesByName(app);

                System.Diagnostics.Process close = procs[0];

                if (!close.HasExited)
                {
                    close.Kill();
                }
            }
            finally
            {
                if (procs != null)
                {
                    foreach (System.Diagnostics.Process p in procs)
                    {
                        p.Dispose();
                    }
                }
            }
        }


    }
}
