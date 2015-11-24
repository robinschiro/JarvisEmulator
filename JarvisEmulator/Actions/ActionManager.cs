using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public struct ActionData
    {
        public string inMessage;
        public string outMessage;
    }

    public enum actionManagerCommands
    {
        OPEN,
        UPDATE,
        CLOSE,
        LOGOUT,
        TAKEPICTURE,
        GREET_USER
    }

    public class ActionManager : IObservable<UserNotification>, IObserver<SpeechData>, IObserver<FrameData>
    {
        RSSManager rssManager;
        string command;
        object commandObject;

        Thread rssManagerThread;

        #region Observer lists

        private List<IObserver<UserNotification>> userNotificationObservers = new List<IObserver<UserNotification>>();

        #endregion
        
        // Username for greeting the user
        private string username;

        public ActionManager()
        {
            // Initialize the username
            username = "";

            // Initialize the RSS Manager
            rssManager = new RSSManager(BroadcastRSSResult);

            // Initialize the RSS Manager Thread TODO: URL????
            rssManagerThread = new Thread( rssManager.PublishRSSString );
        }

        public bool BroadcastRSSResult( RSSData info )
        {
            // Notify the user that no action was taken
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.RSS_DATA, username, info.parsedString));

            return true;
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

        public void CommandOpenApplication( string app )
        {
            Process proc = new Process();
            string appName = commandObject.ToString(); ;

            if (appName.Contains(".exe"))
            {
                appName = appName.Remove(appName.Length - 4);
            }

            Process[] procname = Process.GetProcessesByName(appName);
            try
            {
                if (procname.Length == 0)//it's not yet open
                {
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = commandObject.ToString();
                    proc.Start();
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.OPENING_APPLICATION, username, app));
                }
                else//it is open
                {
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ALREADY_OPENED, username, app));
                }

            }
            catch ( Exception e )
            {
                // Notify the user that no action was taken
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ALREADY_OPENED, username, app));
            }
        }

        public void CommandCloseApplication( string app )
        {
            System.Diagnostics.Process[] procs = null;
            try
            {
                procs = System.Diagnostics.Process.GetProcessesByName(commandObject.ToString());

                System.Diagnostics.Process close = procs[0];

                if (!close.HasExited)
                {
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.CLOSING_APPLICATION, username, app));

                    close.Kill();
                }
            }
            catch ( Exception e )
            {
                // Notify the user that no action was taken
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.NO_APP_TO_CLOSE, username, app));
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

        public void CommandTakePhoto()
        {

        }

        public void CommandRSSUpdate()
        {

        }

        public void CommandQuestionAsked()
        {

        }
        public void Obtain()
        {

        }

        // Use these two lines to make the rss manager parse the needed string and send it back
        //rssManager.provideURL("whatever url rss");
        //rssManagerThread.Start();
        // TODO: Check that the rssManagerThread has ended when trying to start it again

        public void ProcessCommand()
        {
            if (commandObject == null)
            {
                // Notify the user of the action
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ERROR, username, "Command object is null."));
                return;
            }
            if (commandObject.Equals(actionManagerCommands.LOGOUT))
            {
                CommandLogout();
            }
            if (command.Equals(actionManagerCommands.UPDATE.ToString()))
            {
                CommandRSSUpdate();
            }
            if (command.Equals(actionManagerCommands.OPEN.ToString()))
            {
                CommandOpenApplication("");
            }
            if (command.Equals(actionManagerCommands.CLOSE.ToString()))
            {
                CommandCloseApplication("");
            }
            if ((command.Equals(actionManagerCommands.GREET_USER.ToString())))
            {
                // Notify the user of the action
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.USER_ENTERED, username, ""));
            }
        }

        public void DisplayError()
        {

        }

        #region Observer Interface methods

        public void OnNext(SpeechData value)
        {
            command = value.Command;
            commandObject = value.CommandValue;

            ProcessCommand();
        }

        public void OnNext(FrameData value)
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

        #endregion
    }
}
