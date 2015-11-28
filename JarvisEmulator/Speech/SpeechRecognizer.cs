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
using System.Threading;

namespace JarvisEmulator
{

    public struct SpeechData
    {
        public Command Command;
        public string CommandValue;
    }

    public class SpeechRecognizer : IObservable<SpeechData>, IObserver<FrameData>, IObserver<ConfigData>
    {
        // Constants.
        private const double MINIMUM_CONFIDENCE = 0.90;

        private SpeechRecognitionEngine speechRecognizer = new SpeechRecognitionEngine();
        private List<Word> words = new List<Word>();
        private User activeUser;

        // This array was created to prevent Jarvis from recognizing the wrong words.
        private string[] similar = new string[] { "ride Jarvis", "fly Jarvis", "hide Jarvis", "try Jarvis", "my harvest" };

        // Default phrases recognized by Jarvis
        private string[] mainCommands = new string[] { "hello Jarvis", "hi Jarvis","howdy Jarvis", "OK Jarvis, how is the weather",
                                               "OK Jarvis goodbye","OK Jarvis bye","OK Jarvis exit", "OK Jarvis see you later",
                                               "OK Jarvis log out", "OK Jarvis take my picture", "OK Jarvis snap", "OK Jarvis cheese", "OK Jarvis selfie"};

        // Grammar management
        private Choices choices = new Choices();
        private Grammar defaultGrammar;
        private Grammar userGrammar;
        private Thread grammarUpdateThread;
        private volatile bool stopGrammarUpdate;

        private List<IObserver<SpeechData>> commandObserver = new List<IObserver<SpeechData>>();

        // Constructor
        public SpeechRecognizer()
        {

        }

        // Set up the recognizer with default commands and turn it on.
        // The recognition engine run asyncronously.
        public void EnableListening()
        {
            // Add initial commands to the recognizer's set of choices.   
            choices.Add(mainCommands);
            choices.Add(similar);

            // Create a grammmar based on the default choices.
            defaultGrammar = new Grammar(new GrammarBuilder(choices));

            speechRecognizer.RequestRecognizerUpdate();
            speechRecognizer.LoadGrammar(defaultGrammar);

            speechRecognizer.SpeechRecognized += SpeechRecognized;

            // Attempt to hook onto an audio input device.
            try
            {
                speechRecognizer.SetInputToDefaultAudioDevice();
            }
            catch ( Exception ex )
            {
                // No audio input device was found.
                return;
            }
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        // Update Jarvis's grammar by adding commands from the command dictionary of the active user.
        // This should only be called when the active user changes.
        public void UpdateGrammar( bool unloadGrammar )
        {
            if ( unloadGrammar && (null != userGrammar) && speechRecognizer.Grammars.Contains(userGrammar) )
            {
                speechRecognizer.UnloadGrammar(userGrammar);
            }

            Choices userChoices = new Choices();

            // Do not continue if is no active user.
            if ( null == activeUser )
            {
                return;
            }

            //Adds commands to the recognizer's dictionary.
            List<String> commandKeys = activeUser.CommandDictionary.Keys.ToList();

            string[] appOpen = new string[commandKeys.Count];
            string[] update = new string[commandKeys.Count];
            string[] close = new string[commandKeys.Count];

            for (int i = 0; i < commandKeys.Count; i++)
            {

                appOpen[i] = "OK Jarvis open " + commandKeys[i];
                update[i] = "OK Jarvis update " + commandKeys[i];
                close[i] = "OK Jarvis close " + commandKeys[i];
            }


            userChoices.Add(appOpen);
            userChoices.Add(update);
            userChoices.Add(close);

            // Build and add the user grammar to the recognizer. 
            userGrammar = new Grammar(new GrammarBuilder(userChoices));
            speechRecognizer.LoadGrammar(userGrammar);
        }

        public void ProcessVoiceInput( string voiceInput )
        {
            Command commandEnum = 0;
            string commandValue = String.Empty;

            if (voiceInput.StartsWith("OK Jarvis"))
            {
                if (voiceInput.Contains("open"))
                {
                    voiceInput = voiceInput.Replace("OK Jarvis open ", "");
                    commandEnum = Command.OPEN;
                    commandValue = getCommandVal(voiceInput);
                }
                else if (voiceInput.Contains("log out"))
                {
                    commandEnum = Command.LOGOUT;
                }
                else if (voiceInput.Contains("close"))
                {
                    voiceInput = voiceInput.Replace("OK Jarvis close ", "");
                    commandValue = getCommandValClose(voiceInput);
                    commandEnum = Command.CLOSE;
                }
                else if (voiceInput.Contains("update"))
                {
                    voiceInput = voiceInput.Replace("OK Jarvis update ", "");
                    commandValue = getCommandVal(voiceInput);
                    commandEnum = Command.UPDATE;
                }
                else if (voiceInput.Contains("take my picture") || voiceInput.Contains("snap") ||
                    voiceInput.Contains("cheese") || voiceInput.Contains("selfie"))
                {
                    commandEnum = Command.TAKEPICTURE;
                }
                else if ( voiceInput.Contains("weather") )
                {
                    commandEnum = Command.GET_WEATHER;
                }
            }

            PublishSpeechData(commandEnum, commandValue);
        }

        public string getCommandVal( string commandKey )
        {
            if (activeUser != null && activeUser.CommandDictionary.ContainsKey(commandKey) )
            {
                return activeUser.CommandDictionary[commandKey];
            }

            return String.Empty;
        }

        public string getCommandValClose( string commandKey )
        {
            if (activeUser != null && activeUser.CommandDictionary.ContainsKey(commandKey) )
            {
                string commandValue = activeUser.CommandDictionary[commandKey];
                if ( commandValue.Contains(".exe") )
                {
                    return commandValue.Remove(commandValue.Length - 4);
                }
                else
                {
                    return commandValue;
                }
            }

            return String.Empty;
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string voiceInput = e.Result.Text;

            if (e.Result.Confidence > MINIMUM_CONFIDENCE)
            {
                if (voiceInput.StartsWith("OK Jarvis"))
                {
                    ProcessVoiceInput(voiceInput);
                }
                else if (voiceInput.StartsWith("hi Jarvis"))
                {
                    PublishSpeechData(Command.GREET_USER, "");
                }
            }
        }

        public IDisposable Subscribe(IObserver<SpeechData> observer)
        {
            return SubscriptionManager.Subscribe(commandObserver, observer);
        }

        private void PublishSpeechData( Command command, string commandValue )
        {
            SpeechData packet = new SpeechData();
            packet.Command = command;
            packet.CommandValue = commandValue;

            SubscriptionManager.Publish(commandObserver, packet);
        }

        // UpdateGrammar takes too long to run synchronously.
        private void RunUpdateGrammarThread( bool unloadGrammar )
        {
            // Wait for the previous thread to stop if it is active.
            if ( null != grammarUpdateThread && grammarUpdateThread.IsAlive )
            {
                grammarUpdateThread.Join();
            }

            // Create new thread.
            ThreadStart threadInfo = delegate () { UpdateGrammar(unloadGrammar); };
            grammarUpdateThread = new Thread(threadInfo);

            // Start the thread.
            grammarUpdateThread.Start();
        }

        // Update the vocabulary of Jarvis based on the current active user.
        // This update should only be made if the active user has changed.
        public void OnNext(FrameData value)
        {
            if ( activeUser != value.ActiveUser )
            {
                activeUser = value.ActiveUser;
                RunUpdateGrammarThread( null != activeUser );
            }
        }

        // When a user has updated the UI, refresh the recognizer with the commands
        // associated with the active user.
        public void OnNext( ConfigData value )
        {
            if ( value.SaveToProfile )
            {
                RunUpdateGrammarThread((null != activeUser) && !value.PerformCleanup);
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


        class Word
        {
            public Word() { }
            public string Text { get; set; }
            public string AttachedText { get; set; }
            public bool IsShellCommand { get; set; }
        }
    }
}
