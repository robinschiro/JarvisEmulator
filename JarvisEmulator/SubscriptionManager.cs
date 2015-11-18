using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JarvisEmulator
{
    public class SubscriptionManager
    {
        private FaceDetector faceDetector;
        private MainWindow userInterface;
        private ConfigurationManager configManager;
        private SpeechRecognizer speechRecognizer;
        private SpeechConstructor speechConstructor;

        public SubscriptionManager(MainWindow userInterface)
        {
            // Initialize the modules.
            this.userInterface = userInterface;
            faceDetector = new FaceDetector();
            configManager = new ConfigurationManager();
            speechRecognizer = new SpeechRecognizer();
            speechConstructor = new SpeechConstructor();
            // TODO: Initialize the ActionManager

            // Create the subscriptions. Subscriptions must be created before the modules are "turned on".
            CreateSubscriptions();

            // Parse configuration data.
            configManager.ParseProfile();

            // Turn on the FaceDetector.
            faceDetector.InitializeCaptureDevice();
            faceDetector.EnableFrameCapturing();

            // Turn on the SpeechRecognizer.
            speechRecognizer.EnableListening();
        }

        // Create all necessary subscriptions between the modules.
        private void CreateSubscriptions()
        {
            // Create subscriptions to the FaceDetector.
            faceDetector.Subscribe(configManager);
            faceDetector.Subscribe(userInterface);

            // Create subscriptions for the MainWindow.
            userInterface.Subscribe(faceDetector);
            userInterface.Subscribe(configManager);

            // Create subscriptions to the ConfigurationManager.
            configManager.Subscribe(userInterface);

            configManager.Subscribe(speechRecognizer);

            // TODO: Create subscriptions to the ActionManager from the Speech Constructor

        }

        #region Observer Pattern Utilities

        public static void Publish<T>( IList<IObserver<T>> observers, T data )
        {
            foreach ( IObserver<T> observer in observers )
            {
                observer.OnNext(data);
            }
        }

        public static IDisposable Subscribe<T>( IList<IObserver<T>> observers, IObserver<T> observer )
        {
            // Add the observer to the list if it is not already there.
            if ( !observers.Contains(observer) )
            {
                observers.Add(observer);
            }

            return null;
        }

        #endregion
    }
}
