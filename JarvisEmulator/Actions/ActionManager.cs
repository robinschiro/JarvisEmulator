﻿using System;
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
        TAKEPICTURE
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

        public void CommandOpenApplication()
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.OPENING_APPLICATION, username, commandObject.ToString()));

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = commandObject.ToString();
            proc.Start();
        }

        public void CommandCloseApplication()
        {
            // Notify the user of the action
            SubscriptionManager.Publish(userNotificationObservers, new UserNotification(NOTIFICATION_TYPE.CLOSING_APPLICATION, username, commandObject.ToString()));

            System.Diagnostics.Process[] procs = null;
            try
            {
                procs = System.Diagnostics.Process.GetProcessesByName(commandObject.ToString());

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
            if (command.Equals(actionManagerCommands.LOGOUT))
            {
                CommandLogout();
            }
            if (command.Equals(actionManagerCommands.UPDATE))
            {
                CommandRSSUpdate();
            }
            if (command.Equals(actionManagerCommands.OPEN))
            {
                CommandOpenApplication();
            }
            if (command.Equals(actionManagerCommands.CLOSE))
            {
                CommandCloseApplication();
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
