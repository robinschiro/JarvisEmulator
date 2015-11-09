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

        public SubscriptionManager(MainWindow userInterface)
        {
            this.userInterface = userInterface;

            // Initialize the FaceDetector.
            faceDetector = new FaceDetector();
            faceDetector.InitializeCapture();
            faceDetector.EnableFrameCapturing();

        }

        public BitmapSource GetCurrentFrame()
        {
            return faceDetector.CurrentFrame;
        }
    }
}
