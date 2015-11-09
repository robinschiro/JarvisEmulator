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
using System.Windows.Media;

namespace JarvisEmulator
{
    public class FaceDetector : IObservable<User>
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
        private volatile MCvAvgComp[][] facesDetected;

        private System.Threading.Timer frameTimer;
        private System.Threading.Timer detectionTimer;
        private SortedSet<IObserver<User>> userObservers = new SortedSet<IObserver<User>>();

        #endregion

        public BitmapSource CurrentFrame;

        [DllImport("gdi32")]
        private static extern int DeleteObject( IntPtr o );

        public FaceDetector()
        {
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");
        }

        public void EnableFrameCapturing()
        {
            // Spawn the timer that populates the video feed with frames.
            frameTimer = new System.Threading.Timer(RetrieveFrame, null, 0, 30);

            // Spawn the timer that performs the detection.
            detectionTimer = new System.Threading.Timer(DetectFaces, null, 0, 100);
        }

        // Initialize face detection.
        public void InitializeCapture()
        {
            try
            {
                // Initialize the capture device.
                grabber = new Capture();

                // Dump the first frame.
                grabber.QueryFrame();
            }
            catch ( Exception ex ) { }
        }

        private void RetrieveFrame( object state )
        {
            CurrentFrame = GetCurrentFrame(true);
            (CurrentFrame as ImageSource).Freeze();
        }

        private void DetectFaces( object state )
        {
            if ( null != this.currentFrame )
            {
                // Convert it to grayscale
                gray = currentFrame.Convert<Gray, Byte>();

                // Create an array of detected faces.
                facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new System.Drawing.Size(20, 20));
            }
        }

        // Retrieve a frame from the capture device, with faces optionally bounded by rectangles.
        public BitmapSource GetCurrentFrame(bool drawDetectionRectangles)
        {
            // Get the current frame from capture device
            grabber.QueryFrame();
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

            // Process the frame in order to detect faces.
            if ( null != facesDetected )
            {
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
            }

            return ToBitmapSource(currentFrame);
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

        public IDisposable Subscribe( IObserver<User> observer )
        {
            // Add the observer to the set.
            userObservers.Add(observer);

            // Provide the existing data to the observer.

            return null;
        }
    }
}
