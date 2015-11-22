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
        public string Command;
        public object CommandValue;
    }   

    public class SpeechRecognizer : IObserver<FrameData>, IObservable<SpeechData>
    {
        SpeechSynthesizer sSynth = new SpeechSynthesizer();
        private PromptBuilder pBuilder = new PromptBuilder();
        private SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        private List<Word> words = new List<Word>();
        private User activeUser;

        String command = "";
        object commandValue = "";

        string[] similar;
        string[] mainCommands;
        Grammar gr;
        Choices sList;

        private List<IObserver<SpeechData>> commandObserver = new List<IObserver<SpeechData>>();

        public SpeechRecognizer()
        {
            sList = new Choices();
            
            //To prevent Jarvis from recognizing the wrong words.
            similar = new string[] {"ride Jarvis", "fly Jarvis","hide Jarvis","try Jarvis",
                "my harvest" };
            mainCommands = new string[] { "hello Jarvis", "hi Jarvis","howdy Jarvis","OK Jarvis",
                "OK Jarvis goodbye","OK Jarvis bye","OK Jarvis exit", "OK Jarvis see you later",
                "OK Jarvis log out", "OK Jarvis open", "OK Jarvis close","OK Jarvis update",
                "OK Jarvis take my picture", "OK Jarvis snap", "OK Jarvis cheese", "OK Jarvis selfie"};
            sList.Add(mainCommands);
            sList.Add(similar);
            
            try
            {

                gr = new Grammar(new GrammarBuilder(sList));

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

        public void updateGrammar()
        {
            Choices acommands = new Choices();

            //Adds commands to the recognizer's dictionary.
            List<String> commandKeys = new List<String>();
            if (activeUser != null)
            {
                commandKeys = new List<String>(activeUser.CommandDictionary.Keys);
            }
            string[] appOpen = new string[commandKeys.Count];
            string[] update = new string[commandKeys.Count];
            
            for (int i = 0; i < commandKeys.Count; i++)
            {

                appOpen[i] = "OK Jarvis open " + commandKeys[i];
                Debug.WriteLine("if it works it should be " + appOpen[i]);
                update[i] = "OK Jarvis update" + commandKeys[i];
            }


            acommands.Add(appOpen);
            acommands.Add(update);
            try
            {
                Grammar agr = new Grammar(new GrammarBuilder(acommands));
                sRecognize.LoadGrammar(agr);
            }
            catch ( Exception ex )
            {

            }

        }


        public void EnableListening()
        {
            // string command = e.Result.Text;
            
            if (command.StartsWith("OK Jarvis"))
            {
                if (command.Contains("open"))
                {
                    command = command.Replace("OK Jarvis open ", "");
                    getCommandVal();
                    command = actionManagerCommands.OPEN.ToString();
                }
                else if (command.Contains("log out"))
                {
                    command = actionManagerCommands.LOGOUT.ToString();
                }
                else if (command.Contains("close"))
                {
                    getCommandVal();
                    command = actionManagerCommands.CLOSE.ToString();
                }
                else if (command.Contains("update"))
                {
                    getCommandVal();
                    command = actionManagerCommands.UPDATE.ToString();
                }
                else if (command.Contains("take my picture") || command.Contains("snap") ||
                    command.Contains("cheese") || command.Contains("selfie"))
                {
                    command = actionManagerCommands.TAKEPICTURE.ToString();
                }
            }
            //ActionManager.ProcessCommand(command, commandObject);
            PublishSpeechData();
        }

        public void getCommandVal()
        {
            Debug.WriteLine("this is what is in command " + command);
            if (activeUser != null && activeUser.CommandDictionary.ContainsKey(command) )
            {
                commandValue = activeUser.CommandDictionary[command];
            }
        }

        public void swap()
        {
            string temp;
            temp = commandValue.ToString();

            commandValue = command;
            command = temp;
        }

        private void SRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            command = e.Result.Text;

            if ( e.Result.Confidence > 0.8 )
            {
                if (command.StartsWith("OK Jarvis"))
                {
                    EnableListening();
                }
                else if (command.StartsWith("hi Jarvis"))
                {
                    command = actionManagerCommands.GREET_USER.ToString();
                    PublishSpeechData();
                }
            }
        }

        // REMOVE THISSSSSS IF THERE'S ANYTHING BETTER
        private bool checkForSimilar(string command)
        {
            return similar.Contains(command);
        }


        public IDisposable Subscribe(IObserver<SpeechData> observer)
        {
            return SubscriptionManager.Subscribe(commandObserver, observer);
        }

        private void PublishSpeechData()
        {
            SpeechData packet = new SpeechData();
            packet.Command = command;
            packet.CommandValue = commandValue;

            SubscriptionManager.Publish(commandObserver, packet);
        }

        public void OnNext(FrameData user)
        {
            User tempUser = activeUser;
            activeUser = user.ActiveUser;
            if (activeUser != tempUser)
            {
                updateGrammar();
            }


            // TODO: Update the vocabulary of Jarvis based on the current active user.
            // This update should only be made if the active user has changed.
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
