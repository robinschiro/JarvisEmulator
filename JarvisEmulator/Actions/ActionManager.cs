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
        TAKEPICTURE
    }

    public class ActionManager : IObservable<ActionData>, IObserver<SpeechData>
    {
        RSSManager rssManager;
        string command;
        object commandObject;


        public ActionManager()
        {
            rssManager = new RSSManager();
        }

        public IDisposable Subscribe(IObserver<ActionData> observer)
        {
            return null;
        }

        public void CommandOpenApplication(String app)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = app;
            proc.Start();
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

        public void CommandLogout()
        {
            System.Diagnostics.Process.Start("shutdown", "-l");
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
        }

        public void DisplayError()
        {

        }

        public void OnNext(SpeechData value)
        {
            command = value.Command;
            commandObject = value.CommandValue;
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
