using SpeechLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    // TODO: Put some strings for the opening and closing app variants
    public class SpeechConstructor : IObserver<UserNotification>
    {
        // Speech synthesizer
        SpVoice verbalizer;

        // Randomizer
        Random randomizer;

        // A list of different ways of greeting the user.
        string[] greetingVariants = { "Hi,", "Hello,", "Good to see you,", "Nice to see you,", "It's always a plasure to see you," };
        // A list of different ways of notifying a log out.
        string[] logoutVariants = { "Good bye,", "See you next time,", "Have a nice day,", "Until next time," };
        // A list of different ways of notifying a warning.
        string[] notificationIntroductionWarning = {"It's not a big deal, but", "Don't worry to much, but", "So you know,", "For your information,", "We have a small problem,", "There's something that need your attention." };
        // A list of different ways of notifying an error.
        string[] notificationIntroductionError = { "An error has happened", "Something terrible has happened.", "This is not a drill.", "We have a big problem," };
        // A list of different ways of outputing the data.
        string[] notificationIntroductionDataOutput = { "About the information you requested,", "Here's what I found about what you asked, sir.", "Here's what I found.", "This should be it." };
        // A list of different ways of notifying when opening an application.
        string[] openingAppVariants = { "" };
        // A list of different ways of notifying when closing an application.
        string[] closingAppVariants = { "" };


        public SpeechConstructor()
        {
            // Initialize all variables
            verbalizer = new SpVoice();
            randomizer = new Random();
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

        private void VoiceResponse(string response)
        {
            verbalizer.Speak(response);
        }
        #endregion

        #region UTILITIES
        private string getRandomStringFromList(string[] list)
        {
            int randomIndex = randomizer.Next(list.Length);

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
                    VoiceResponse(getRandomStringFromList(notificationIntroductionDataOutput) + notification);
                    break;
                case NOTIFICATION_TYPE.WARNING:
                    VoiceResponse(getRandomStringFromList(notificationIntroductionWarning) + notification);
                    break;
                case NOTIFICATION_TYPE.ERROR:
                    VoiceResponse(getRandomStringFromList(notificationIntroductionError) + notification);
                    break;
                case NOTIFICATION_TYPE.OPENING_APPLICATION:
                    OpeningApp(notification.data);
                    break;
                case NOTIFICATION_TYPE.CLOSING_APPLICATION:
                    OpeningApp(notification.data);
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
        #endregion
    }
}
