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
using System.IO;
using System.Threading;

namespace JarvisEmulator
{

    public struct FrameData
    {
        public BitmapSource Frame;
        public Image<Gray, byte> Face;
        public User ActiveUser;
    }

    public class FaceDetector : IObservable<FrameData>, IObserver<UIData>, IObserver<ConfigData>
    {
        #region Private Variables

        private Image<Bgr, Byte> currentFrame;
        private Capture grabber;
        private HaarCascade face;
        private MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        private Image<Gray, byte> gray = null;

        private string pathToTrainingImagesFolder;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        private List<Guid> trainingImageGuids = new List<Guid>();
        private const int MAX_CACHE_SIZE = 20;

        private MCvAvgComp[][] facesDetected;
        private ConcurrentBag<Rectangle> faceRectangleBag = new ConcurrentBag<Rectangle>();
        private bool drawDetectionRectangles = false;
        private List<User> users;
        private User activeUser;

        private volatile bool stopFrameProcessing;


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
            Thread frameProcessor = new Thread(PerformFrameProcessing);
            frameProcessor.Start();

#if USE_MULTITHREADING
            // Spawn the timer that performs the detection.
            detectionTimer = new System.Threading.Timer(DetectFaces, null, 0, 100);
#endif
        }

        #region Frame Processing

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
                try
                {
                    // Convert frame to grayscale
                    gray = currentFrame.Convert<Gray, Byte>();

                    // Create an array of detected faces.                
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

        // Process frames from the capture device.
        // This function should be called on a thread separate from the main thread.
        private void PerformFrameProcessing()
        {
            while ( !stopFrameProcessing )
            {
                ProcessCurrentFrame();
                Thread.Sleep(10);
            }
        }

        // Process the current frame from the capture device to determine the active user.
        public void ProcessCurrentFrame( object state = null )
        {
            // Verify that the frame grabber exists.
            if ( null == grabber )
            {
                // Notify user that the capture device has not been initialized.
                return;
            }

            // Retrieve the current frame.
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            Image<Gray, byte> result = null;
            BitmapSource frameBitmap = null;

#if !USE_MULTITHREADING
            DetectFaces();
#endif
            try
            {
                // Process the frame in order to detect faces.
                // TODO: Collection is modifed mid-enumeration. Need to fix.
                foreach ( Rectangle rect in faceRectangleBag )
                {
                    // Retrieve just the user's face from the frame.
                    result = currentFrame.Copy(rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                    lock ( trainingImages )
                    {
                        // Perform recognition on the result.
                        if ( 0 != trainingImages.Count )
                        {
                            // Create a set of criteria for the recognizer.
                            MCvTermCriteria termCrit = new MCvTermCriteria(trainingImages.Count, 0.001);

                            // Create a recognizer based on the training images and term criteria.
                            EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), trainingImageGuids.ToArray(), MAX_CACHE_SIZE, 5000, ref termCrit);

                            // Determine the active user.
                            Guid userGuid = recognizer.Recognize(result);
                            
                            lock (users)
                            {
                                activeUser = users.Find(user => user.Guid == userGuid);
                            }
                        }
                        else
                        {
                            activeUser = null;
                        }
                    }

                    // Draw a rectangle around each detected face.
                    if ( drawDetectionRectangles )
                    {
                        currentFrame.Draw(rect, new Bgr(System.Drawing.Color.Red), 2);
                    }
                    
                }
            }
            catch (Exception ex) { }
            finally
            {
                if ( null != currentFrame )
                {
                    // Convert the frame to something viewable within WPF and freeze it so that it can be displayed.
                    frameBitmap = ToBitmapSource(currentFrame);
                    frameBitmap.Freeze();
                }
            }

            // Create a frame data packet.
            FrameData packet = new FrameData();
            packet.Frame = frameBitmap;
            packet.Face = result;
            packet.ActiveUser = activeUser;

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

        #endregion

        // Retrieve the training images of all users from the training images folder.
        private void RetrieveTrainingImages()
        {
            if ( null != pathToTrainingImagesFolder && Directory.Exists(pathToTrainingImagesFolder) )
            {
                // Since the trainingImages lists is about to modifed, lock it.
                lock ( trainingImages )
                {
                    // Clear the training image and guid lists.
                    trainingImages.Clear();
                    trainingImageGuids.Clear();

                    // Load each training image file into the list of images.
                    // At the same time, update the corresponding Guid list.
                    DirectoryInfo[] userFolders = (new DirectoryInfo(pathToTrainingImagesFolder)).GetDirectories();
                    foreach ( DirectoryInfo folder in userFolders )
                    {
                        FileInfo[] files = folder.GetFiles();
                        Guid userGuid = new Guid(folder.Name);
                        foreach ( FileInfo file in files )
                        {
                            trainingImages.Add(new Image<Gray, byte>(file.FullName));
                            trainingImageGuids.Add(userGuid);
                        }
                    }
                }
            }
        }

        #region Observer Pattern Required Methods

        public IDisposable Subscribe( IObserver<FrameData> observer )
        {
            return SubscriptionManager.Subscribe(frameObservers, observer);
        }

        public void OnNext( UIData value )
        {
            drawDetectionRectangles = value.DrawDetectionRectangles;

            if ( value.SaveToProfile )
            {
                lock (users)
                {
                    users = value.Users;
                }
                pathToTrainingImagesFolder = value.PathToTrainingImages;
            }

            if ( value.RefreshTrainingImages )
            {
                RetrieveTrainingImages();
            }

            if ( value.PerformCleanup )
            {
                stopFrameProcessing = true;
                if ( null != grabber )
                {
                    grabber.Dispose();
                }
            }
        }

        public void OnNext( ConfigData value )
        {
            drawDetectionRectangles = value.DrawDetectionRectangles;
            users = value.Users;
            pathToTrainingImagesFolder = value.PathToTrainingImages;

            RetrieveTrainingImages();
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
