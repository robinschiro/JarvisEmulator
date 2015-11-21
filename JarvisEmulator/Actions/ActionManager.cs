using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public struct ActionData
    {
        public string Message;
    }

    // TODO: Speech recognizer observer functions, RSSManager observer functions
    public class ActionManager : IObservable<ActionData>, IObservable<UserNotification>, IObserver<ConfigData>
    {
        RSSManager rssManager;

        #region Observer lists

        private List<IObserver<UserNotification>> userNotificationObservers = new List<IObserver<UserNotification>>();

        #endregion
        
        // Username for greeting the user
        private string username;

        public ActionManager()
        {
            rssManager = new RSSManager();
            username = "";
        }

        public IDisposable Subscribe(IObserver<ActionData> observer)
        {
            return null;
        }

        public IDisposable Subscribe(IObserver<UserNotification> observer)
        {
            return SubscriptionManager.Subscribe(userNotificationObservers, observer);
        }

        public void CommandLogout()
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.LOG_OUT, username));

            System.Diagnostics.Process.Start("shutdown", "-l");
        }

        public void CommandOpenApplication(String app)
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.OPENING_APPLICATION, username, app));

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = app;
            proc.Start();
        }

        public void CommandCloseApplication(String app)
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.CLOSING_APPLICATION, username, app));

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

        public void OnNext(ConfigData value)
        {
            if (value.ActiveUser != null)
            {
                // Save the username every time it receives configdata
                username = value.ActiveUser.ToString();
            }
        }

        public void OnError(Exception error)
        {
            return;
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
