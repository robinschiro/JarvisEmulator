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
using System.Collections.ObjectModel;

namespace JarvisEmulator
{
    public struct UIData
    {
        public bool DrawDetectionRectangles;
        public bool HaveJarvisGreetUser;
        public List<User> Users;
        public string PathToTrainingImages;

        public bool SaveToProfile;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IObservable<UIData>, IObserver<BitmapSource>, IObserver<ConfigData>
    {
        private Timer frameTimer;
        private BitmapSource currentFrame;
        private ObservableCollection<User> users;

        #region Observer Lists

        private List<IObserver<UIData>> uiObservers = new List<IObserver<UIData>>();

        #endregion

        public MainWindow()
        {
            // Create the user interface.
            InitializeComponent();

            // Create the Subscription Manager.
            SubscriptionManager subManager = new SubscriptionManager(this);

            // Set the items source of the User Selection dropdown menu.
            cboxUserSelection.ItemsSource = users;

            // Spawn the timer that populates the video feed with frames.
            frameTimer = new Timer(DisplayFrame, null, 0, 30);
        }

        #region Events

        private void DisplayFrame( object state )
        {
            // Invoke Dispatcher in order to update the UI.
            this.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                // Only display the frames if the tab is selected.
                if ( tabVideoFeed == tabControlMain.SelectedItem as TabItem )
                {
                    imgVideoFeed.Source = currentFrame;
                }
            }));
        }

        private void BrowseForFile( object sender, RoutedEventArgs e )
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();

            tboxTGitPath.Text = dialog.FileName;
        }

        private void PublishUIData( object sender, RoutedEventArgs e )
        {
            UIData packet = new UIData();
            packet.DrawDetectionRectangles = chkEnableTracking.IsChecked ?? false;
            packet.HaveJarvisGreetUser = chkGreetUsers.IsChecked ?? false;
            packet.Users = users.ToList<User>();

            SubscriptionManager.Publish(uiObservers, packet);
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

        // Update the user interface using information from the Configuration Manager.
        // This should only be called at application initialization.
        public void OnNext( ConfigData value )
        {
            // Populate the user collection.
            users = new ObservableCollection<User>(value.Users);

            // Update the 


        }

        public void OnNext( BitmapSource value )
        {
            currentFrame = value;
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe( IObserver<UIData> observer )
        {
            return SubscriptionManager.Subscribe(uiObservers, observer);
        }
    }
}
