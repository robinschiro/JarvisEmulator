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
    public class SpeechRecognizer
    {
        List<User> users = new List<User>();
        SpeechSynthesizer sSynth = new SpeechSynthesizer();
        PromptBuilder pBuilder = new PromptBuilder();
        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        List<Word> words = new List<Word>();

        public SpeechRecognizer()
        {

        }

        public void EnableListening()
        {

        }        

        private void button2_Click(object sender, RoutedEventArgs e)
        {
          //  button2.IsEnabled = false;
         //   button3.IsEnabled = true;
            Choices sList = new Choices();

            //put these in a text file
            string[] frFile = File.ReadAllLines(Environment.CurrentDirectory + "\\actions.txt");

            sList.Add(new string[] { "hello","hello Jarvis", "hi","hi Jarvis","howdy Jarvis","howdy", "ola", "ola Jarvis",
                "OK Jarvis goodbye","OK Jarvis bye","OK Jarvis buy","OK Jarvis close","OK Jarvis exit", "OK Jarvis see you later", "OK Jarvis by",
                "OK Jarvis log out", "OK Jarvis open",
                "OK Jarvis take my picture", "OK Jarvis take my photo", "OK Jarvis cheese", "OK Jarvis take my selfie", "OK Jarvis i feel pretty today"

                , "ok jarvis how's the weather", "oh stop it you", "ok jarvis open youtube", "thank you jarvis"
            });
            try
            {
                foreach (string line in frFile)
                {
                    if (line.StartsWith("--") || line == String.Empty) continue;

                    var parts = line.Split(new char[] { '|' });

                    // add commandItem to the list for later lookup or execution
                    words.Add(new Word() { Text = parts[0], AttachedText = parts[1], IsShellCommand = (parts[2] == "true") });

                    // add the text to the known choices of speechengine
                    sList.Add(parts[0]);

                }

                Grammar gr = new Grammar(new GrammarBuilder(sList));

                sRecognize.RequestRecognizerUpdate();
                sRecognize.LoadGrammar(gr);

                //DictationGrammar dict = new DictationGrammar();// not that great -

                //sRecognize.LoadGrammar(dict);//gr or dict
                sRecognize.SpeechRecognized += SRecognize_SpeechRecognized;
                sRecognize.SetInputToDefaultAudioDevice();
                sRecognize.RecognizeAsync(RecognizeMode.Multiple);

            }
            catch
            {
                return;
            }
        }

        private void SRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Text;
            Random random = new Random();
            int randomNumber = random.Next(0, 6);


            if (command.ToLower() == "hello" || command.ToLower() == "hello jarvis" || command.ToLower() == "high" || command.ToLower() == "high jarvis" ||
                command.ToLower() == "hi" || command.ToLower() == "hi jarvis" || command.ToLower() == "howdy" | command.ToLower() == "ola" || command.ToLower() == "howdy jarvis")
            {
                //will be replaced with call to speech constructor
                sSynth.Speak("Hello");

            }

            //for opening applications. this is a test and will later be put into the if statement directly below
            if (command.StartsWith("OK Jarvis open"))
            {
                command = command.Replace("OK Jarvis open", "");

                //get words from txt file
                if (command == "word")
                {
                    Process proc = new Process();
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = "winword.exe";
                    proc.Start();

                    sSynth.Speak("yes sire. I'm opening " + command);
                    //break;
                }

            }

            if (command.StartsWith("OK Jarvis"))
            {
                command = command.Replace("OK Jarvis ", "");
                if (command.ToLower() == "goodbye" || command.ToLower() == "goodbye" || command.ToLower() == "buy" || command.ToLower() == "bye" ||
                    command.ToLower() == "by" || command.ToLower() == "close")
                {
                    //will be replaced with call to speech constructor
                    if (randomNumber == 1)
                        sSynth.Speak("goodbye");
                    else if (randomNumber == 2)
                        sSynth.Speak("see you later, crocodile");
                    else if (randomNumber == 3)
                        sSynth.Speak("bye");
                    else if (randomNumber == 4)
                        sSynth.Speak("adios");
                    else if (randomNumber == 5)
                        sSynth.Speak("parting is of such sweet sorrow");
                    else
                    {
                        sSynth.Speak("take care, smiley face");

                    }
                 //fix this   Close();
                }

                if (command.ToLower() == "log out")
                {
                    //will be replaced with call to speech constructor
                    sSynth.Speak("yes my lord, i hope that you have saved all your work because it is too late now");
                    ActionManager.CommandLogout();

                }

                if (command.ToLower() == "take my picture")
                {

                }

            }

        //    textBox.Text = textBox.Text + " " + e.Result.Text.ToString();
        //    textBox.Text = textBox.Text + "\n";

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
