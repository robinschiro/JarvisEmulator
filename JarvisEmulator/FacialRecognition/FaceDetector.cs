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
using System.Drawing;

namespace JarvisEmulator
{
    public class FaceDetector : IObservable<User>, IObservable<BitmapSource>, IObserver<UIData>, IObserver<ConfigData>
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
        private List<User> users;
        private int ContTrain, NumLabels, t;
        private string name, names = null;
        private MCvAvgComp[][] facesDetected;
        private volatile List<Rectangle> faceRectangles = new List<Rectangle>();
        private bool drawDetectionRectangles = false;

        private System.Threading.Timer frameTimer;
        private System.Threading.Timer detectionTimer;

        #region Observer Lists

        private List<IObserver<User>> userObservers = new List<IObserver<User>>();
        private List<IObserver<BitmapSource>> frameObservers = new List<IObserver<BitmapSource>>();

        #endregion


        #endregion

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
            frameTimer = new System.Threading.Timer(GetCurrentFrame, null, 0, 30);

            // Spawn the timer that performs the detection.
            detectionTimer = new System.Threading.Timer(DetectFaces, null, 0, 100);
        }

        // Initialize face detection.
        public void InitializeCaptureDevice()
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

        private void DetectFaces( object state )
        {
            if ( null != currentFrame )
            {
                // Convert it to grayscale
                gray = currentFrame.Convert<Gray, Byte>();

                // Create an array of detected faces.
                // TODO: Memory access/write error occurs here.
                facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new System.Drawing.Size(20, 20));

                // Update the list storing the current face rectangles.
                faceRectangles.Clear();
                foreach ( MCvAvgComp f in facesDetected[0] )
                {
                    faceRectangles.Add(f.rect);
                }
            }
        }

        // Retrieve a frame from the capture device, with faces optionally bounded by rectangles.
        public void GetCurrentFrame( object state )
        {
            // Get the current frame from capture device
            grabber.QueryFrame();
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

            // Process the frame in order to detect faces.
            if ( null != faceRectangles )
            {
                //Action for each element detected
                foreach ( Rectangle rect in faceRectangles )
                {
                    //result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    // Draw a rectangle around each detected face.
                    if ( drawDetectionRectangles )
                    {
                        currentFrame.Draw(rect, new Bgr(System.Drawing.Color.Red), 2);
                    }
                }
            }

            // Convert the frame to something viewable within WPF and freeze it so that it can be displayed.
            BitmapSource frameBitmap = ToBitmapSource(currentFrame);
            frameBitmap.Freeze();

            // Send the frame to all frame observers.
            SubscriptionManager.Publish(frameObservers, frameBitmap);
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
                  BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        public IDisposable Subscribe( IObserver<User> observer )
        {
            return SubscriptionManager.Subscribe(userObservers, observer);
        }

        public IDisposable Subscribe( IObserver<BitmapSource> observer )
        {
            return SubscriptionManager.Subscribe(frameObservers, observer);
        }

        public void OnNext( UIData value )
        {
            drawDetectionRectangles = value.DrawDetectionRectangles;
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnNext( ConfigData value )
        {
            throw new NotImplementedException();
        }
    }
}
