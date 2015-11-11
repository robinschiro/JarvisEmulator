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

        public SubscriptionManager(MainWindow userInterface)
        {
            this.userInterface = userInterface;
            this.configManager = new ConfigurationManager();

            // Initialize the FaceDetector.
            faceDetector = new FaceDetector();
            faceDetector.InitializeCapture();
            faceDetector.EnableFrameCapturing();

            // Create the subscriptions.
            CreateSubscriptions();
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
