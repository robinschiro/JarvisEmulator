//#define USE_MULTITHREADING

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
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace JarvisEmulator
{

    public struct FrameData
    {
        public BitmapSource Frame;
        public Image<Gray, byte> Face;
    }

    public class FaceDetector : IObservable<User>, IObservable<FrameData>, IObserver<UIData>, IObserver<ConfigData>
    {
        #region Private Variables

        private Image<Bgr, Byte> currentFrame;
        private Capture grabber;
        private HaarCascade face;
        private MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        private Image<Gray, byte> gray = null;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();

        private MCvAvgComp[][] facesDetected;
        private ConcurrentBag<Rectangle> faceRectangleBag = new ConcurrentBag<Rectangle>();
        private bool drawDetectionRectangles = false;
        private List<User> users;


        private System.Threading.Timer frameTimer;
        private System.Threading.Timer detectionTimer;

        #region Observer Lists

        private List<IObserver<User>> userObservers = new List<IObserver<User>>();
        private List<IObserver<FrameData>> frameObservers = new List<IObserver<FrameData>>();

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
            frameTimer = new System.Threading.Timer(GetCurrentFrame, null, 0, 100);

#if USE_MULTITHREADING
            // Spawn the timer that performs the detection.
            detectionTimer = new System.Threading.Timer(DetectFaces, null, 0, 100);
#endif
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

        [HandleProcessCorruptedStateExceptions]
        private void DetectFaces( object state = null )
        {
            if ( null != currentFrame )
            {
                // Convert it to grayscale
                gray = currentFrame.Convert<Gray, Byte>();

                // Create an array of detected faces.                
                try
                {
                    facesDetected = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new System.Drawing.Size(20, 20));
                }
                catch (Exception ex) { }

                // Update the list storing the current face rectangles.
                faceRectangleBag = new ConcurrentBag<Rectangle>();
                foreach ( MCvAvgComp f in facesDetected[0] )
                {
                    faceRectangleBag.Add(f.rect);
                }
            }
        }

        // Retrieve a frame from the capture device, with faces optionally bounded by rectangles.
        public void GetCurrentFrame( object state )
        {
            // Get the current frame from capture device
            grabber.QueryFrame();
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            Image<Gray, byte> result = null;
            BitmapSource frameBitmap = null;

#if !USE_MULTITHREADING
            DetectFaces();
#endif
            try
            {
                // Process the frame in order to detect faces.
                if ( null != faceRectangleBag )
                {
                    // Action for each element detected
                    // TODO: Collection is modifed mid-enumeration. Need to fix.
                    foreach ( Rectangle rect in faceRectangleBag )
                    {
                        result = currentFrame.Copy(rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                        // Draw a rectangle around each detected face.
                        if ( drawDetectionRectangles )
                        {
                            currentFrame.Draw(rect, new Bgr(System.Drawing.Color.Red), 2);
                        }
                    }
                }

                // Convert the frame to something viewable within WPF and freeze it so that it can be displayed.
                frameBitmap = ToBitmapSource(currentFrame);
                frameBitmap.Freeze();
            }
            catch (Exception ex) { }

            // Create a frame data packet.
            FrameData packet = new FrameData();
            packet.Frame = frameBitmap;
            packet.Face = result;

            // Send the frame to all frame observers.
            SubscriptionManager.Publish(frameObservers, packet);
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

        public IDisposable Subscribe( IObserver<FrameData> observer )
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
            users = value.Users;
            drawDetectionRectangles = value.DrawDetectionRectangles;
        }
    }
}
