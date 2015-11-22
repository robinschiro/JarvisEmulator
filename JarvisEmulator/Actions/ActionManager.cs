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

    public enum actionManagerCommands
    {
        OPEN,
        UPDATE,
        CLOSE,
        LOGOUT,
        TAKEPICTURE,
        GREET_USER
    }

    public class ActionManager : IObservable<ActionData>, IObserver<SpeechData>, IObserver<FrameData>
    {
        RSSManager rssManager;
        string command;
        object commandObject;

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
            System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName(app);

            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = app;
                proc.Start();

                // Notify the user of the action
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.OPENING_APPLICATION, username, app));
            }
            catch( Exception e )
            {
                // Notify the user that no action was taken
                SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.ALREADY_OPENED, username, app));
            }
        }

        public void CommandCloseApplication(String app)
        {
            System.Diagnostics.Process[] procs = null;
            try
            {
                procs = System.Diagnostics.Process.GetProcessesByName(app);

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

        public void ProcessCommand()
        {
            if (commandObject == null)
                return;

            if (commandObject.Equals(actionManagerCommands.LOGOUT))
            {
                CommandLogout();
            }
            if (commandObject.Equals(actionManagerCommands.UPDATE))
            {
                CommandRSSUpdate();
            }
            if (commandObject.Equals(actionManagerCommands.OPEN))
            {
                //gets the application name
                command = command.Replace("OK Jarvis open", "");
                //search through txt doc for the application location/.exe file
                CommandOpenApplication(command);
            }
            if (commandObject.Equals(actionManagerCommands.CLOSE))
            {
                //gets the application name
                command = command.Replace("OK Jarvis close", "");
                //search through txt doc for the application location/.exe file
                CommandCloseApplication(command);
            }
            if ((commandObject.Equals(actionManagerCommands.GREET_USER)))
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
