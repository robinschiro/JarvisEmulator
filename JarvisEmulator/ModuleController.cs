using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JarvisEmulator
{
    public class ModuleController
    {
        private FaceDetector faceDetector;

        public ModuleController(MainWindow userInterface)
        {
            // Initialize the FaceDetector.
            faceDetector = new FaceDetector();
            faceDetector.InitializeCapture();
        }

        public BitmapSource GetCurrentFrame()
        {
            return faceDetector.GetCurrentFrame(true);
        }
    }
}
