using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private Image<Gray, byte> facePicture;

        #region RSS Management

        private RSSManager rssManager;
        private Thread rssManagerThread;
        private const string WEATHER_URL = "http://weather.yahooapis.com/forecastrss?p=";
        private int zipCode = 32826;
        private bool greetUser = false;

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

        public void CommandOpenApplication( string nickname, string appLocation )
        {
            Process proc = new Process();

            if ( appLocation.Contains(".exe") )
            {
                appLocation = appLocation.Remove(appLocation.Length - 4);
            }

            Process[] procname = Process.GetProcessesByName(appLocation);
            try
            {
                if ( procname.Length == 0 )//it's not yet open
                {
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = appLocation;
                    proc.Start();
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.OPENING_APPLICATION, username, nickname));
                }
                else//it is open
                {
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ALREADY_OPENED, username, nickname));
                }

            }
            catch ( Exception e )
            {
                // Notify the user that no action was taken
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ALREADY_OPENED, username, nickname));
            }
        }

        public void CommandCloseApplication( string nickname, string appLocation )
        {
            // The provided string will be the path to the executable file. Parse the executable name from the path.
            string exeName = Path.GetFileNameWithoutExtension(appLocation);        

            Process[] procs = null;
            try
            {
                procs = Process.GetProcessesByName(exeName);

                Process close = procs[0];

                if ( !close.HasExited )
                {
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.CLOSING_APPLICATION, username, nickname));

                    close.Kill();
                }
            }
            catch ( Exception e )
            {
                // Notify the user that no action was taken
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.NO_APP_TO_CLOSE, username, nickname));
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

        public void CommandTakePhoto(string loc)
        {
            // Store a picture of the face provided by the latest frame data packet.
            if (null != facePicture)
            {
                facePicture.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), username + "_Selfie.bmp"));
            }  
        }

        public void CommandRSSUpdate( string nickname, string URL )
        {
            // Wait for the RSS processing thread to stop if it is active.
            if (rssManagerThread.IsAlive)
            {
                rssManagerThread.Join();
            }

            // Create new thread.
            rssManagerThread = new Thread(rssManager.PublishRSSString);

            // Provide the URL and start the thread.
            rssManager.NickName = nickname;
            rssManager.URL = URL;
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

        public void ProcessCommand( Command command, string commandKey, User user )
        {
            string commandValue = String.Empty;
            if ( null == commandKey )
            {
                // Notify the user of the action
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ERROR, username, "Command key is null."));
                return;
            }
            else if ( user != null && user.CommandDictionary.ContainsKey(commandKey) )
            {
                commandValue = user.CommandDictionary[commandKey];
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
                    CommandRSSUpdate(commandKey, commandValue);
                    break;
                }

                case Command.OPEN:
                {
                    CommandOpenApplication(commandKey, commandValue);
                    break;
                }

                case Command.CLOSE:
                {
                    CommandCloseApplication(commandKey, commandValue);
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
                    CommandRSSUpdate("", WEATHER_URL + zipCode);
                    break;
                }
                case Command.TAKEPICTURE:
                {
                    CommandTakePhoto(commandValue);
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
            ProcessCommand(value.Command, value.CommandKey, value.ActiveUser);
        }

        public void OnNext( FrameData value )
        {
            // Maintain reference to face picture.
            facePicture = value.Face;

            string original = username;
            if ( value.ActiveUser != null )
            {
                // Save the username every time it receives configdata
                username = value.ActiveUser.ToString();
            }
            else
            {
                username = "";
            }

            if ( !String.IsNullOrEmpty(username) && username != original )
            {
                // Greet the new active user if the application is configured that way
                if ( greetUser )
                {
                    // Notify the user of the action
                    SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.USER_ENTERED, username, ""));
                }
            }
        }

        public void OnNext( ConfigData value )
        {
            zipCode = value.ZipCode;
            greetUser = value.HaveJarvisGreetUser;
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
