using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows;

namespace JarvisEmulator
{
    public class FaceDetector
    {
        #region Private Variables

        private Image<Bgr, Byte> currentFrame;
        private Capture grabber;
        private HaarCascade face;
        private HaarCascade eye;
        private MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        private Image<Gray, byte> result, TrainedFace = null;
        private Image<Gray, byte> gray = null;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        private List<string> labels = new List<string>();
        private List<string> NamePersons = new List<string>();
        private int ContTrain, NumLabels, t;
        private string name, names = null;

        #endregion

        [DllImport("gdi32")]
        private static extern int DeleteObject( IntPtr o );

        public FaceDetector()
        {
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");
        }

        // Initialize face detection.
        public void InitializeCapture()
        {
            // Initialize the capture device.
            grabber = new Capture();

            // Dump the first frame.
            grabber.QueryFrame();
        }

        // Retrieve a frame from the capture device, with faces optionally bounded by rectangles.
        public BitmapSource GetCurrentFrame(bool drawDetectionRectangles)
        {
            // Get the current frame from capture device
            grabber.QueryFrame();
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

            // Process the frame in order to detect faces.
            {
                // Convert it to grayscale
                gray = currentFrame.Convert<Gray, Byte>();

                // Create an array of detected faces.
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new System.Drawing.Size(20, 20));

                //Action for each element detected
                foreach ( MCvAvgComp f in facesDetected[0] )
                {
                    //result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    // Draw a rectangle around each detected face.
                    if ( drawDetectionRectangles )
                    {
                        currentFrame.Draw(f.rect, new Bgr(System.Drawing.Color.Red), 2);
                    }
                }

                return ToBitmapSource(currentFrame);
            }
        }

        // Convert a bitmap image to a BitmapSource, which WPF can use to display the image.
        public static BitmapSource ToBitmapSource( IImage image )
        {
            using ( System.Drawing.Bitmap source = image.Bitmap )
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

    }
}
