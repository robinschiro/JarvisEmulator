using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using Emgu.CV.Structure;
using Emgu.CV;

namespace JarvisEmulator
{
    public struct UIData
    {
        public bool DrawDetectionRectangles;
        public bool HaveJarvisGreetUser;
        public List<User> Users;
        public string PathToTrainingImages;

        public bool SaveToProfile;
        public bool RefreshTrainingImages;
        public bool PerformCleanup;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IObservable<UIData>, IObserver<FrameData>, IObserver<ConfigData>
    {
        private Timer frameTimer;
        private BitmapSource currentFrame;
        private Image<Gray, byte> facePicture;
        private ObservableCollection<User> users;
        public ObservableCollection<User> Users
        {
            get { return users; }
            set { users = value; }
        }

        private User selectedUser;
        public User SelectedUser
        {
            get { return selectedUser; }
            set
            {
                selectedUser = value;
                NotifyPropertyChanged("SelectedUser");
            }
        }

        private User activeUser;
        public User ActiveUser
        {
            get { return activeUser; }
            set
            {
                activeUser = value;
                NotifyPropertyChanged("ActiveUser");
            }
        }

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
            cboxUserSelection.SelectedIndex = 0;
            cboxUserSelection_SelectionChanged();

            // Set the data context of various controls.
            pnlMain.DataContext = this;

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

        private void cboxUserSelection_SelectionChanged( object sender = null, SelectionChangedEventArgs e = null)
        {
            // Update the binding of the Commands Listview.
            object selectedItem = cboxUserSelection.SelectedItem;
            if ( null != selectedItem )
            {
                SelectedUser = selectedItem as User;
            }           
        }

        private void btnModifyEntry_Click( object sender, RoutedEventArgs e )
        {
            // Retrieve the item currently selected in the listview.
            object selectedItem = lvCommandDictionary.SelectedItem;
            if ( null != selectedItem )
            { 
                KeyValuePair<string, string> commandPair = (KeyValuePair<string, string>)selectedItem;

                TwoEntryDialog dialog = new TwoEntryDialog("Modify Command Entry", "Trigger Word:", "URL/Path:", commandPair.Key, commandPair.Value);
                dialog.ShowDialog();

                if ( true == dialog.Result )
                { 
                    // Update the key/value pair.
                    object selectedUser = cboxUserSelection.SelectedItem;
                    if ( null != selectedUser )
                    {
                        User user = (selectedUser as User);
                        user.CommandDictionary.Remove(commandPair.Key);
                        user.CommandDictionary[dialog.EntryOne] = dialog.EntryTwo;
                    }
                }
            }
        }

        private void btnAddEntry_Click( object sender, RoutedEventArgs e )
        {
            TwoEntryDialog dialog = new TwoEntryDialog("Add Command Entry", "Trigger Word:", "URL/Path:");
            dialog.ShowDialog();

            // Create the key/value pair.
            if ( true == dialog.Result )
            {
                object selectedUser = cboxUserSelection.SelectedItem;
                if ( null != selectedUser )
                {
                    (selectedUser as User).CommandDictionary[dialog.EntryOne] = dialog.EntryTwo;
                }
            }
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

                // TODO: Delete user's picture folder.
            }
        }

        private void btnTrainUser_Click( object sender, RoutedEventArgs e )
        {
            // Send the user to the Video Feed tab and lock him in there until he clicks "Finish".
            if ( MessageBoxResult.Yes == MessageBox.Show("You will now be taken to the Video Feed tab to take pictures of the selected user's face. " +
                                                         "Are you sure you want to do this?", "Training Mode", MessageBoxButton.YesNo, MessageBoxImage.Information) )
            {
                // TODO: Handle exception for invalid directory.
                // Create user training folder if it does not already exist.
                string pathUserTrainingFolder = Path.Combine(tboxTrainingImagesPath.Text, selectedUser.Guid.ToString());
                if ( !Directory.Exists( pathUserTrainingFolder ) )
                {
                    Directory.CreateDirectory(pathUserTrainingFolder);
                }

                ToggleTrainingMode(true);
            }
        }

        private void btnFinish_Click( object sender, RoutedEventArgs e )
        {
            ToggleTrainingMode(false);

            PublishUIData(refreshTrainingImages: true);
        }

        private void btnDeleteUser_Click( object sender, RoutedEventArgs e )
        {
            if ( MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to delete the selected user?", "Delete User", MessageBoxButton.YesNo, MessageBoxImage.Question) )
            {
                // Remove the user from the collection.
                users.Remove(SelectedUser);

                // Update the combobox.
                cboxUserSelection.SelectedIndex = 0;

                // Update the public property.
                object selectedItem = cboxUserSelection.SelectedItem;
                SelectedUser = (null != selectedItem) ? (selectedItem as User) : null;

                // Save to profile.
                PublishUIData(saveToProfile: true);
            }
        }

        private void btnNewUser_Click( object sender, RoutedEventArgs e )
        {
            TwoEntryDialog dialog = new TwoEntryDialog("Create User", "First Name:", "Last Name:");
            dialog.ShowDialog();

            if ( true == dialog.Result )
            {
                // Add the new user to the collection of users.
                User newUser = new User(Guid.NewGuid(), dialog.EntryOne, dialog.EntryTwo, new ObservableDictionary<string, string>());
                users.Add(newUser);

                // Make this user the currently selected user.
                cboxUserSelection.SelectedItem = newUser;

                // Save to profile.
                PublishUIData(saveToProfile: true);
            }
        }

        private void chkEnableTracking_Click( object sender, RoutedEventArgs e )
        {
            PublishUIData();
        }

        private void btnBrowse_Click( object sender, RoutedEventArgs e )
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = tboxTrainingImagesPath.Text;
            dialog.ShowDialog();

            tboxTrainingImagesPath.Text = dialog.SelectedPath;

            PublishUIData(saveToProfile: true, refreshTrainingImages: true);
        }

        private void btnSnapshot_Click( object sender, RoutedEventArgs e )
        {
            string pathUserTrainingFolder = Path.Combine(tboxTrainingImagesPath.Text, selectedUser.Guid.ToString());
            string fileName = String.Empty;

            // Retrieve the name of the latest picture to be saved in this folder.
            // The new file name should be incremented by one.
            List<FileInfo> files = (new DirectoryInfo(pathUserTrainingFolder)).GetFiles().ToList<FileInfo>();
            if ( 0 == files.Count )
            {
                fileName = "1.bmp";
            }
            else
            {
                try
                {
                    List<int> fileNameInts = fileNameInts = files.Select(file => Convert.ToInt32(Path.GetFileNameWithoutExtension(file.Name))).ToList();
                    fileName = (fileNameInts.Max() + 1) + ".bmp";
                }
                catch
                {
                    MessageBox.Show("Training images folder has invalid files. Please clear this directory or select a new one.");
                }
            }

            // Store a picture of the face that is contained within the first rectangle of the rectangle list.
            if ( null != facePicture )
            {
                facePicture.Save(Path.Combine(pathUserTrainingFolder, fileName));
            }

        }

        private void Window_Closing( object sender, CancelEventArgs e )
        {
            PublishUIData(saveToProfile: true, performCleanup: true);
        }


        #endregion

        private void ToggleTrainingMode( bool on )
        {
            tabConfig.IsSelected = !on;
            tabConfig.IsEnabled = !on;
            tabVideoFeed.IsSelected = on;
            gridTrainingButtons.Visibility = on ? Visibility.Visible : Visibility.Hidden;

            string message = "";
            if ( on )
            {
                message = "You are now in Training Mode. The Selected User should pose in front of the webcam " +
                          "at various angles and perhaps in various lighting conditions. For each pose, click the '" +
                          btnSnapshot.Content + "' button. When you are finished, press the '" + btnFinish.Content + "' button.";
            }
            else
            {
                message = "You are leaving Training Mode.";
            }

            MessageBox.Show(message, "Training Mode");
        }

        // Refresh the list presented in a listview.
        private void RefreshItemControl(ItemsControl control)
        {
            if ( null != control.ItemsSource )
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(control.ItemsSource);
                view.Refresh();
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
            }       
        }

        public void OnNext( FrameData value )
        {
            currentFrame = value.Frame;
            facePicture = value.Face;
            ActiveUser = value.ActiveUser;
        }

        public IDisposable Subscribe( IObserver<UIData> observer )
        {
            return SubscriptionManager.Subscribe(uiObservers, observer);
        }

        private void PublishUIData( bool saveToProfile = false, bool refreshTrainingImages = false, bool performCleanup = false )
        {
            UIData packet = new UIData();
            packet.DrawDetectionRectangles = chkEnableTracking.IsChecked ?? false;
            packet.HaveJarvisGreetUser = chkGreetUsers.IsChecked ?? false;
            packet.PathToTrainingImages = tboxTrainingImagesPath.Text;
            packet.Users = users.ToList<User>();
            packet.SaveToProfile = saveToProfile;
            packet.RefreshTrainingImages = refreshTrainingImages;
            packet.PerformCleanup = performCleanup;

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
    }
}
