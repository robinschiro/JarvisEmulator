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
using System.ComponentModel;

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
    public partial class MainWindow : Window, INotifyPropertyChanged, IObservable<UIData>, IObserver<BitmapSource>, IObserver<ConfigData>
    {
        private Timer frameTimer;
        private BitmapSource currentFrame;
        private ObservableCollection<User> users;

        public event PropertyChangedEventHandler PropertyChanged;

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
            cboxUserSelection.SelectedIndex = 0;
            cboxUserSelection_SelectionChanged();

            // Spawn the timer that populates the video feed with frames.
            frameTimer = new Timer(DisplayFrame, null, 0, 30);
        }

        private void NotifyPropertyChanged( string propertyName = "" )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region Events

        private void NewUserButton_Click( object sender, RoutedEventArgs e )
        {

        }

        private void SaveButton_Click( object sender, RoutedEventArgs e )
        {
            // Update all subscribers.
            PublishUIData();
        }

        private void cboxUserSelection_SelectionChanged( object sender = null, SelectionChangedEventArgs e = null)
        {
            // Update the binding of the Commands Listview.
            User selectedUser = (cboxUserSelection.SelectedItem as User);
            if ( null != selectedUser )
            {
                lvCommandDictionary.ItemsSource = selectedUser.CommandDictionary;
            }           
        }

        private void btnModifyEntry_Click( object sender, RoutedEventArgs e )
        {
            // Retrieve the item currently selected in the listview.
            object selectedItem = lvCommandDictionary.SelectedItem;
            if ( null != selectedItem )
            { 
                KeyValuePair<string, string> commandPair = (KeyValuePair<string, string>)selectedItem;

                CommandEntryDialog dialog = new CommandEntryDialog(commandPair.Key, commandPair.Value);
                dialog.ShowDialog();

                // Update the key/value pair.
                object selectedUser = cboxUserSelection.SelectedItem;
                if ( null != selectedUser )
                {
                    (selectedUser as User).CommandDictionary[commandPair.Key] = dialog.CommandValue;
                }

                // Refresh the list view.
                RefreshItemControl(lvCommandDictionary);
            }
        }

        private void btnAddEntry_Click( object sender, RoutedEventArgs e )
        {
            CommandEntryDialog dialog = new CommandEntryDialog();
            dialog.ShowDialog();

            // Update the key/value pair.
            object selectedUser = cboxUserSelection.SelectedItem;
            if ( null != selectedUser )
            {
                (selectedUser as User).CommandDictionary[dialog.CommandKey] = dialog.CommandValue;
            }

            // Refresh the list view.
            RefreshItemControl(lvCommandDictionary);
        }

        private void btnDeleteEntry_Click( object sender, RoutedEventArgs e )
        {
            // Retrieve the item currently selected in the listview.
            object selectedItem = lvCommandDictionary.SelectedItem;
            if ( null != selectedItem )
            {
                KeyValuePair<string, string> commandPair = (KeyValuePair<string, string>)selectedItem;

                // Update the key/value pair.
                object selectedUser = cboxUserSelection.SelectedItem;
                if ( null != selectedUser )
                {
                    (selectedUser as User).CommandDictionary.Remove(commandPair.Key);
                }

                // Refresh the list view.
                RefreshItemControl(lvCommandDictionary);
            }
        }

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

        private void BrowseForFolder( object sender, RoutedEventArgs e )
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = tboxTrainingImagesPath.Text;
            dialog.ShowDialog();

            tboxTrainingImagesPath.Text = dialog.SelectedPath;
        }

        #endregion

        // Refresh the list presented in a listview.
        private void RefreshItemControl(ItemsControl control)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(control.ItemsSource);
            view.Refresh();
        }

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

        #region Observer Patter Requirements

        // Update the user interface using information from the Configuration Manager.
        // This should only be called at application initialization.
        public void OnNext( ConfigData value )
        {
            if ( value.IsInit )
            {
                // Populate the user collection.
                users = new ObservableCollection<User>(value.Users);

                // Update the user interface.
                chkEnableTracking.IsChecked = value.DrawDetectionRectangles;
                chkGreetUsers.IsChecked = value.HaveJarvisGreetUser;
                tboxTrainingImagesPath.Text = value.PathToTrainingImages;

                // Refresh listview.
            }       
        }

        public void OnNext( BitmapSource value )
        {
            currentFrame = value;
        }

        public IDisposable Subscribe( IObserver<UIData> observer )
        {
            return SubscriptionManager.Subscribe(uiObservers, observer);
        }

        private void PublishUIData( object sender = null, RoutedEventArgs e = null )
        {
            UIData packet = new UIData();
            packet.DrawDetectionRectangles = chkEnableTracking.IsChecked ?? false;
            packet.HaveJarvisGreetUser = chkGreetUsers.IsChecked ?? false;
            packet.Users = users.ToList<User>();

            SubscriptionManager.Publish(uiObservers, packet);
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

        private void btnTrainUser_Click( object sender, RoutedEventArgs e )
        {

        }

        private void btnDeleteUser_Click( object sender, RoutedEventArgs e )
        {

        }
    }
}
