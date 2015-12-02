// ===============================
// AUTHOR: Robin Schiro
// PURPOSE: To use OpenCV to detect faces in a frame of a video feed.
// ===============================
// Change History:
//
// RS   11/5/2015  Created class
//
//==================================

#define USE_MULTITHREADING

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

    public class FaceDetector : IObservable<FrameData>, IObserver<ConfigData>
    {
        #region Private Variables

        #region Constants

        private const int MAX_CACHE_SIZE = 20;
        private const int EIGEN_THRESHOLD = 3000;

        #endregion

        private Image<Bgr, Byte> currentFrame;
        private Capture grabber;
        private HaarCascade face;
        private MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        private Image<Gray, byte> gray = null;

        private string pathToTrainingImagesFolder;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        private List<Guid> trainingImageGuids = new List<Guid>();

        private MCvAvgComp[][] facesDetected;
        private ConcurrentBag<Rectangle> faceRectangleBag = new ConcurrentBag<Rectangle>();
        private bool drawDetectionRectangles = false;
        private List<User> users = new List<User>();
        private User activeUser;
        Image<Gray, byte> facePic = null;

        // Thread Management
        private Thread frameProcessor;
        private Thread frameCapturing;
        private volatile bool stopFrameProcessing;
        private object lockBagObject = new object();

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
#if USE_MULTITHREADING
            // Spawn the thread that performs the capturing.
            frameCapturing = new Thread(PerformFrameCapturing);
            frameCapturing.Start();
#endif

            // Spawn the thread that performs the processing.
            frameProcessor = new Thread(PerformFrameProcessing);
            frameProcessor.Start();
        }

        #region Threaded Functions

        private void PerformFrameCapturing()
        {
            while ( !stopFrameProcessing )
            {
                CaptureFrame();
                Thread.Sleep(10);
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

        #endregion

        #region Frame Processing

        // Get access to the user's webcam.
        public void InitializeCaptureDevice()
        {
            try
            {
                // Initialize the capture device.
                grabber = new Capture();

                // Dump the first frame.
                currentFrame = grabber.QueryFrame();
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
                lock ( lockBagObject )
                {
                    faceRectangleBag = new ConcurrentBag<Rectangle>();
                    foreach ( MCvAvgComp f in facesDetected[0] )
                    {
                        faceRectangleBag.Add(f.rect);
                    }
                }
            }
        }

        // Retrieve frames from the capture device.
        private void CaptureFrame()
        {
            try
            {
                if ( stopFrameProcessing )
                {
                    return;
                }

                Image<Bgr, byte> modifiedFrame;

                // Verify that the frame grabber exists.
                if ( null == grabber )
                {
                    // Notify user that the capture device has not been initialized.
                    return;
                }

                // Retrieve the current frame.
                lock ( currentFrame )
                {
                    currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    modifiedFrame = currentFrame.Copy();
                }

                lock ( lockBagObject )
                {
                    foreach ( Rectangle rect in faceRectangleBag )
                    {
                        // This action causes problems for an unknown reason sometimes.
                        Image<Gray, byte> tempFacePic = null;
                        try
                        {
                            tempFacePic = modifiedFrame.Copy(rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        }
                        catch ( Exception ex ) { }
                        finally
                        {
                            if ( null != tempFacePic )
                            {
                                facePic = tempFacePic;
                            }
                        }

                        // Draw a rectangle around each detected face.
                        if ( drawDetectionRectangles )
                        {
                            modifiedFrame.Draw(rect, new Bgr(System.Drawing.Color.Red), 2);
                        }
                    }
                }

                // Convert the frame to something viewable within WPF and freeze it so that it can be displayed.
                BitmapSource frameBitmap = ToBitmapSource(modifiedFrame);
                frameBitmap.Freeze();

                // Create a frame data packet.
                FrameData packet = new FrameData();
                packet.Frame = frameBitmap;
                packet.Face = facePic;
                packet.ActiveUser = activeUser;

                // Send the frame to all frame observers.
                SubscriptionManager.Publish(frameObservers, packet);
            }
            catch ( Exception ex ) { }


        }

        // Process the current frame from the capture device to determine the active user.
        public void ProcessCurrentFrame( object state = null )
        {
            if ( stopFrameProcessing )
            {
                return;
            }

#if !USE_MULTITHREADING
            // Verify that the frame grabber exists.
            if ( null == grabber )
            {
                // Notify user that the capture device has not been initialized.
                return;
            }

            // Retrieve the current frame.
            currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

#endif
            // Detect all faces in the current frame.
            DetectFaces();

            // Local objects used for recognition.
            Image<Gray, byte> result = null;
            BitmapSource frameBitmap = null;

            // Recognize the active user in the frame.
            try
            {
                Rectangle largestRect = new Rectangle(0,0,1,1);
                bool faceDetected = false;

                // Process the frame in order to detect faces.
                lock ( lockBagObject )
                {
                    // Choose the largest rectangle in the bag.
                    foreach ( Rectangle rect in faceRectangleBag )
                    {
                        if ( (rect.Width * rect.Height) > (largestRect.Width * largestRect.Height) )
                        {
                            largestRect = rect;
                            faceDetected = true;
                        }
                    }
                }

                // Retrieve just the user's face from the frame.
                lock ( currentFrame )
                {
                    result = currentFrame.Copy(largestRect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                }

                lock ( trainingImages )
                {
                    // Perform recognition on the result.
                    if ( 0 != trainingImages.Count )
                    {
                        // Create a set of criteria for the recognizer.
                        MCvTermCriteria termCrit = new MCvTermCriteria(trainingImages.Count, 0.001);

                        // Create a recognizer based on the training images and term criteria.
                        EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), trainingImageGuids.ToArray(), MAX_CACHE_SIZE, EIGEN_THRESHOLD, ref termCrit);

                        // Determine the active user.
                        Guid userGuid = recognizer.Recognize(faceDetected, result);

                        lock ( users )
                        {
                            activeUser = users.Find(user => user.Guid == userGuid);
                        }
                    }
                    else
                    {
                        activeUser = null;
                    }
                }

#if !USE_MULTITHREADING
                // Draw a rectangle around each detected face.
                if ( drawDetectionRectangles )
                {
                    currentFrame.Draw(largestRect, new Bgr(System.Drawing.Color.Red), 2);
                }
#endif

            }
            catch (Exception ex) { }

#if !USE_MULTITHREADING
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
#endif
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

        public void OnNext( ConfigData value )
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

                // Wait for threads to terminate.
                frameCapturing.Join();
                frameProcessor.Join();

                if ( null != grabber )
                {
                    grabber.Dispose();
                }
            }
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
