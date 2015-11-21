using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace JarvisEmulator
{

    public struct SpeechData
    {
        public string command;
        public object commandObject;
    }

   

    public class SpeechRecognizer : IObserver<ConfigData>, IObservable<SpeechData>
    {
        SpeechSynthesizer sSynth = new SpeechSynthesizer();
        PromptBuilder pBuilder = new PromptBuilder();
        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        List<Word> words = new List<Word>();
        User ActiveUser;

        String command = "";
        object commandObject ;

        private List<IObserver<SpeechData>> commandObserver = new List<IObserver<SpeechData>>();

        public SpeechRecognizer()
        {
            Choices sList = new Choices();
            
            //To prevent Jarvis from recognizing the wrong words.
            string[] similar = {"ride Jarvis", "fly Jarvis","hide Jarvis","try Jarvis",
                "my harvest" };
            string[] mainCommands = { "hello Jarvis", "hi Jarvis","howdy Jarvis","OK Jarvis",
                "OK Jarvis goodbye","OK Jarvis bye","OK Jarvis exit", "OK Jarvis see you later",
                "OK Jarvis log out", "OK Jarvis open", "OK Jarvis close","OK Jarvis update",
                "OK Jarvis take my picture", "OK Jarvis snap", "OK Jarvis cheese", "OK Jarvis selfie"};
            

            //Adds commands to the recognizer's dictionary.
            List<String> commandKeys = new List<String>(); ;
            if (ActiveUser != null)
            {
                commandKeys = new List<String>(ActiveUser.CommandDictionary.Keys);
            }
            string[] appOpen = new string[commandKeys.Count];
            
            for (int i = 0; i < commandKeys.Count; i++)
            {
                appOpen[i] = "OK Jarvis open" + commandKeys[i];
            }

            sList.Add(mainCommands);
            sList.Add(similar);
            sList.Add(appOpen);

            try
            {

                Grammar gr = new Grammar(new GrammarBuilder(sList));

                sRecognize.RequestRecognizerUpdate();
                sRecognize.LoadGrammar(gr);

                sRecognize.SpeechRecognized += SRecognize_SpeechRecognized;
                sRecognize.SetInputToDefaultAudioDevice();
                sRecognize.RecognizeAsync(RecognizeMode.Multiple);

            }
            catch
            {
                return;
            }
        }


        public void EnableListening()
        {
            // string command = e.Result.Text;
            
            if (command.StartsWith("OK Jarvis"))
            {
                if (command.Contains("open"))
                {
                    commandObject = actionManagerCommands.OPEN;
                }
                if (command.Contains("log out"))
                {
                    commandObject = actionManagerCommands.LOGOUT;
                }
                if (command.Contains("close"))
                {
                    commandObject = actionManagerCommands.CLOSE;
                }
                if (command.Contains("update"))
                {
                    commandObject = actionManagerCommands.UPDATE;
                }
                if (command.Contains("take my picture") || command.Contains("snap") ||
                    command.Contains("cheese") || command.Contains("selfie"))
                {
                    commandObject = actionManagerCommands.TAKEPICTURE;
                }

            }
            //ActionManager.ProcessCommand(command, commandObject);
            PublishSpeechData();
        }


        private void SRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            command = e.Result.Text;
            Random random = new Random();
            int randomNumber = random.Next(0, 6);

            if (command.StartsWith("hi Jarvis"))
            {
                sSynth.Speak("Hello");
            }

            if (command.StartsWith("OK Jarvis"))
            {
                EnableListening();
            }

        }


        public IDisposable Subscribe(IObserver<SpeechData> observer)
        {
            return SubscriptionManager.Subscribe(commandObserver, observer);
        }

        private void PublishSpeechData()
        {
            SpeechData packet = new SpeechData();
            packet.command= command;
            packet.commandObject = commandObject;

            SubscriptionManager.Publish(commandObserver, packet);
        }

        public void OnNext(ConfigData user)
        {
            ActiveUser = user.ActiveUser;
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        class Word
        {
            public Word() { }
            public string Text { get; set; }
            public string AttachedText { get; set; }
            public bool IsShellCommand { get; set; }
        }
    }
}
