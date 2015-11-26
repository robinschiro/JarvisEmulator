using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace JarvisEmulator
{
    public enum Command
    {
        OPEN,
        UPDATE,
        CLOSE,
        LOGOUT,
        TAKEPICTURE,
        GREET_USER,
        GET_WEATHER
    }

    public class ActionManager : IObservable<UserNotification>, IObserver<SpeechData>, IObserver<FrameData>, IObserver<ConfigData>
    {
        #region Private Members

        #region RSS Management

        private RSSManager rssManager;
        private Thread rssManagerThread;
        private const string WEATHER_URL = "http://weather.yahooapis.com/forecastrss?p=";
        private int zipCode = 32826;
        #endregion

        #region Observer lists

        private List<IObserver<UserNotification>> userNotificationObservers = new List<IObserver<UserNotification>>();

        #endregion

        #endregion

        // Username for greeting the user
        private string username = "";

        public ActionManager()
        {
            // Initialize the RSS Manager
            rssManager = new RSSManager(BroadcastRSSResult);

            // Initialize the RSS Manager Thread TODO: URL????
            rssManagerThread = new Thread(rssManager.PublishRSSString);
        }

        public bool BroadcastRSSResult( RSSData info )
        {
            // Notify the user that no action was taken
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.RSS_DATA, username, info.parsedString));

            return true;
        }

        public IDisposable Subscribe( IObserver<UserNotification> observer )
        {
            return SubscriptionManager.Subscribe(userNotificationObservers, observer);
        }

        public void CommandLogout()
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.LOG_OUT, username));

            if ( MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to log out? All applications will be closed.", "Are you sure?", MessageBoxButton.YesNo) )
            {
                Process.Start("shutdown", "-l");
            }
        }

        public void CommandOpenApplication( string app )
        {
            Process proc = new Process();

            if ( app.Contains(".exe") )
            {
                app = app.Remove(app.Length - 4);
            }

            Process[] procname = Process.GetProcessesByName(app);
            try
            {
                if ( procname.Length == 0 )//it's not yet open
                {
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = app;
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
            Process[] procs = null;
            try
            {
                procs = Process.GetProcessesByName(app);

                Process close = procs[0];

                if ( !close.HasExited )
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
                if ( procs != null )
                {
                    foreach ( Process p in procs )
                    {
                        p.Dispose();
                    }
                }
            }
        }

        public void CommandTakePhoto()
        {

        }

        public void CommandRSSUpdate( string URL )
        {
            // Wait for the RSS processing thread to stop if it is active.
            if (rssManagerThread.IsAlive)
            {
                rssManagerThread.Join();
            }

            // Create new thread.
            rssManagerThread = new Thread(rssManager.PublishRSSString);

            // Provide the URL and start the thread.
            rssManager.provideURL(URL);
            rssManagerThread.Start();
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

        public void ProcessCommand( Command command, string commandValue )
        {
            if ( null == commandValue )
            {
                // Notify the user of the action
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ERROR, username, "Command object is null."));
                return;
            }

            switch ( command )
            {
                case Command.LOGOUT:
                {
                    CommandLogout();
                    break;
                }

                case Command.UPDATE:
                {
                    CommandRSSUpdate(commandValue);
                    break;
                }

                case Command.OPEN:
                {
                    CommandOpenApplication(commandValue);
                    break;
                }

                case Command.CLOSE:
                {
                    CommandCloseApplication(commandValue);
                    break;
                }

                case Command.GREET_USER:
                {
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.USER_ENTERED, username, ""));
                    break;
                }

                case Command.GET_WEATHER:
                {
                    CommandRSSUpdate(WEATHER_URL + zipCode);
                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        public void DisplayError()
        {

        }

        #region Observer Interface methods

        public void OnNext( SpeechData value )
        {
            ProcessCommand(value.Command, value.CommandValue);
        }

        public void OnNext( FrameData value )
        {
            if ( value.ActiveUser != null )
            {
                // Save the username every time it receives configdata
                username = value.ActiveUser.ToString();
            }
        }

        public void OnNext( ConfigData value )
        {
            zipCode = value.ZipCode;
        }

        public void OnError( Exception error )
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
