using SpeechLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    // TODO: Put some strings for the opening and closing app variants
    public class SpeechConstructor : IObserver<UserNotification>, IObserver<ConfigData>
    {
        // Speech synthesizer
        SpVoice verbalizer;

        // Queue that holds the messages to be spoken
        Queue<string> SpokenMessages = new Queue<string>();
        // Thread that speaks the messages
        Thread SpeechOutputThread;

        private bool stopTalking = false;

        // Randomizer
        Random randomizer;

        // A list of different ways of greeting the user.
        string[] greetingVariants = { "High, ", "Hello, ", "Good to see you, ", "Nice to see you, ", "It's always a pleasure to see you, " };
        // A list of different ways of notifying a log out.
        string[] logoutVariants = { "Good bye, ", "See you next time, ", "Have a nice day, ", "Until next time, " };
        // A list of different ways of notifying a warning.
        string[] notificationIntroductionWarning = {"It's not a big deal, but ", "Don't worry to much, but ", "So you know, ", "For your information, ", "We have a small problem, ", "There's something that need your attention." };
        // A list of different ways of notifying an error.
        string[] notificationIntroductionError = { "An error has happened ", "Something terrible has happened. ", "This is not a drill. ", "We have a big problem," };
        // A list of different ways of outputing the data.
        string[] notificationIntroductionDataOutput = { "About the information you requested, ", "Here's what I found about what you asked, sir. ", "Here's what I found. " };
        // A list of different ways of notifying when opening an application.
        string[] openingAppVariants = { "Opening ", "I will open", "Here it is for you," };
        // A list of different ways of notifying when opening an application.
        string[] openedAlreadyAppVariants = { "Already opened.", "The application you requested is already opened.", "I think I already did that." };
        // A list of different ways of notifying when closing an application.
        string[] closingAppVariants = { "Closing ", "I will close ", "No more of ", "Bye bye, " };
        // A list of different ways of notifying when closing an application.
        string[] noAppToCloseVariants = { "I can't find that application anywhere.", "No application with that name opened.", "Can't do the impossible, that application is not opened." };

        // So it doesn't repeat twice the same random introduction
        public int lastIndex = 0;


        public SpeechConstructor()
        {
            // Initialize all variables
            verbalizer = new SpVoice();
            randomizer = new Random();

            SpeechOutputThread = new Thread(this.ProcessSpokenMessages);

            SpeechOutputThread.Start();
        }

        #region MODULE     
        private void ProcessActionOutput()
        {

        }
        private void ConstructResponse()
        {

        }
        private void GreetUser(string UserName)
        {
            VoiceResponse(getRandomStringFromList(greetingVariants) + UserName);
        }
        private void LoggingOut(string UserName)
        {
            VoiceResponse(getRandomStringFromList(logoutVariants) + UserName);
        }
        private void OpeningApp(string application)
        {
            VoiceResponse(getRandomStringFromList(openingAppVariants) + application);
        }
        private void ClosingApp(string application)
        {
            VoiceResponse(getRandomStringFromList(closingAppVariants) + application);
        }
        private void NotOpeningApp(string application)
        {
            VoiceResponse(getRandomStringFromList(openedAlreadyAppVariants) + application);
        }
        private void NotClosingApp(string application)
        {
            VoiceResponse(getRandomStringFromList(noAppToCloseVariants) + application);
        }

        private bool VoiceResponse(string response)
        {
            if (!stopTalking)
            {
                // Put the response in the message queue
                SpokenMessages.Enqueue(response);

                return true;
            }

            return false;
        }

        private void ProcessSpokenMessages()
        {
            // While the speech constructor is on
            while(!stopTalking)
            {
                // If there's any message in the queue
                if( SpokenMessages.Count > 0 )
                {
                    string message = SpokenMessages.Dequeue();

                    verbalizer.Speak(message);
                }
                else
                {
                    Thread.Sleep(150);
                }
            }
        }

        private void StopProcessingMessages()
        {
            stopTalking = true;

            SpeechOutputThread.Join();
        }
        #endregion

        #region UTILITIES
        private string getRandomStringFromList(string[] list)
        {
            int randomIndex;

            while (true)
            {
                randomIndex = randomizer.Next(list.Length);

                // Make sure the picked number is not the same as the last number
                //  that increases the illusion of randomness on the user
                if( randomIndex != lastIndex )
                {
                    lastIndex = randomIndex;
                    break;
                }
            }

            return list[randomIndex];
        }
        #endregion

        #region OBSERVER INTERFACE
        public void OnNext(UserNotification notification)
        {
            switch( notification.type )
            {
                case NOTIFICATION_TYPE.USER_ENTERED:
                    GreetUser(notification.userName);
                    break;
                case NOTIFICATION_TYPE.LOG_OUT:
                    LoggingOut(notification.userName);
                    break;
                case NOTIFICATION_TYPE.RSS_DATA:
                    VoiceResponse(getRandomStringFromList(notificationIntroductionDataOutput) + notification.data);
                    break;
                case NOTIFICATION_TYPE.WARNING:
                    VoiceResponse(getRandomStringFromList(notificationIntroductionWarning) + notification.data);
                    break;
                case NOTIFICATION_TYPE.ERROR:
                    VoiceResponse(getRandomStringFromList(notificationIntroductionError) + notification.data);
                    break;
                case NOTIFICATION_TYPE.OPENING_APPLICATION:
                    OpeningApp(notification.data);
                    break;
                case NOTIFICATION_TYPE.CLOSING_APPLICATION:
                    ClosingApp(notification.data);
                    break;
                case NOTIFICATION_TYPE.ALREADY_OPENED:
                    NotOpeningApp(notification.data);
                    break;
                case NOTIFICATION_TYPE.NO_APP_TO_CLOSE:
                    NotClosingApp(notification.data);
                    break;
            }
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnNext(ConfigData value)
        {
            if( value.PerformCleanup )
            {
                StopProcessingMessages();
            }
        }
        #endregion
    }
}
