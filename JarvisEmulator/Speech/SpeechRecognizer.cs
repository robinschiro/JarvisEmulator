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
    public class SpeechRecognizer : IObserver<ConfigData>
    {
        SpeechSynthesizer sSynth = new SpeechSynthesizer();
        PromptBuilder pBuilder = new PromptBuilder();
        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        List<Word> words = new List<Word>();
        User ActiveUser;

        String command = "";


        public SpeechRecognizer()
        {
            Choices sList = new Choices();

            //change to get dictionary from config?

            string[] greeting = { "hello Jarvis", "hi Jarvis", "howdy Jarvis" };

            sList.Add(new string[] { "hello Jarvis", "hi Jarvis","howdy Jarvis",
                "OK Jarvis", "goodbye","bye","exit","see you later",
                "OK Jarvis goodbye","OK Jarvis bye","OK Jarvis exit", "OK Jarvis see you later",
                "OK Jarvis log out", "OK Jarvis open", "OK Jarvis close",
                "OK Jarvis update",
                "OK Jarvis take my picture", "OK Jarvis snap", "OK Jarvis cheese", "OK Jarvis selfie"});

            sList.Add(new String[] { "OK Jarvis open" + "word" });

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
            object commandObject = "";
            if (command.StartsWith("OK Jarvis"))
            {
                if (command.Contains("open"))
                {
                    commandObject = actionManager.OPEN;
                }
                if (command.Contains("log out"))
                {
                    commandObject = actionManager.LOGOUT;
                }
                if (command.Contains("close"))
                {
                    commandObject = actionManager.CLOSE;
                }
                if (command.Contains("update"))
                {
                    commandObject = actionManager.UPDATE;
                }
                if (command.Contains("take my picture") || command.Contains("snap") ||
                    command.Contains("cheese") || command.Contains("selfie"))
                {
                    commandObject = actionManager.TAKEPICTURE;
                }

            }
            ActionManager.ProcessCommand(command, commandObject);

        }


        private void SRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            command = e.Result.Text;
            Random random = new Random();
            int randomNumber = random.Next(0, 6);

            if (command.StartsWith("hi Jarvis"))
            {
                sSynth.Speak("Hello my lord sire sir, the most attractive entity in all the universe. i'm surprised the universe hasn't imploded by your gravitational pull");
            }

            if (command.StartsWith("OK Jarvis"))
            {

                EnableListening();
            }

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
