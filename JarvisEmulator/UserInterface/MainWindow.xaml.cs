using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Threading;

namespace JarvisEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SubscriptionManager subManager;
        private Timer frameTimer;


        public MainWindow()
        {
            // Create the user interface.
            InitializeComponent();

            // Create the Subscription Manager.
            subManager = new SubscriptionManager(this);

            // Spawn the timer that populates the video feed with frames.
            frameTimer = new Timer(DisplayFrame, null, 0, 30);
        }

        #region Button Events

        private void btnDisplayFeed_Click( object sender, RoutedEventArgs e )
        {
            // During Application Idle (perhaps run this on a separate thread at some point),
            // update the video feed with frames from the FaceDetector.
            imgVideoFeed.Source = subManager.GetCurrentFrame();
        }
        private void DisplayFrame( object state )
        {
            // Invoke Dispatcher in order to update the UI.
            this.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                imgVideoFeed.Source = subManager.GetCurrentFrame();
            }));
        }

        private void BrowseForFile( object sender, RoutedEventArgs e )
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();

            tboxTGitPath.Text = dialog.FileName;
        }

        #endregion

        // Create a new thread to run a function that cannot be run on the same thread invoking CreateNewThread().
        public Thread CreateNewThread( Action<Object> action, object data = null, string name = "" )
        {
            ThreadStart l_Start = delegate () { Dispatcher.Invoke(DispatcherPriority.Normal, action, data); };
            Thread l_NewThread = new Thread(l_Start);

            if ( !String.IsNullOrEmpty(name) )
            {
                l_NewThread.Name = name;
            }

            l_NewThread.Start();

            return l_NewThread;
        }
    }
}
