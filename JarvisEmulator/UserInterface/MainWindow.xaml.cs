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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IObservable<ConfigData>, IObserver<FrameData>, IObserver<ConfigData>
    {
        private Timer frameTimer;
        private BitmapSource currentFrame;
        private Image<Gray, byte> facePicture;
        private int zipCode;
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

        private List<IObserver<ConfigData>> uiObservers = new List<IObserver<ConfigData>>();

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

        #region User and Command Modification

        private void btnDeleteUser_Click( object sender, RoutedEventArgs e )
        {
            if ( null != selectedUser )
            {
                if ( MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to delete the selected user?", "Delete User", MessageBoxButton.YesNo, MessageBoxImage.Question) )
                {
                    // Remove the user from the collection.
                    users.Remove(selectedUser);

                    // Delete user's picture folder.
                    string userFolder = Path.Combine(tboxTrainingImagesPath.Text, selectedUser.Guid.ToString());
                    if ( Directory.Exists(userFolder) )
                    {
                        Directory.Delete(userFolder, true);
                    }

                    // Save to profile.
                    PublishUIData(saveToProfile: true);

                    // Update the combobox.
                    cboxUserSelection.SelectedIndex = 0;
                }
            }
        }

        private void btnModifyUser_Click( object sender, RoutedEventArgs e )
        {
            if ( null != selectedUser )
            {
                TwoEntryDialog dialog = new TwoEntryDialog("Modify User", "First Name:", "Last Name:", selectedUser.FirstName, selectedUser.LastName);
                dialog.ShowDialog();

                if ( true == dialog.Result )
                {
                    // Update the key/value pair.
                    SelectedUser.FirstName = dialog.EntryOne;
                    SelectedUser.LastName = dialog.EntryTwo;

                    // Remove the user from the observable collection and then re-add.
                    // Unfortunately, this is required in order to trigger the binding update.
                    users.Remove(selectedUser);
                    users.Add(selectedUser);

                    // The currently selected user should be the user that was just modified.
                    cboxUserSelection.SelectedItem = selectedUser;

                    // Save to profile.
                    PublishUIData(saveToProfile: true);
                }
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
                    if ( null != selectedUser )
                    {
                        // Update the key/value pair.
                        selectedUser.CommandDictionary.Remove(commandPair.Key);
                        selectedUser.CommandDictionary[dialog.EntryOne] = dialog.EntryTwo;

                        // Save to profile.
                        PublishUIData(saveToProfile: true);
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
                if ( null != selectedUser )
                {
                    selectedUser.CommandDictionary[dialog.EntryOne] = dialog.EntryTwo;

                    // Save to profile.
                    PublishUIData(saveToProfile: true);
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

                if ( null != selectedUser )
                {
                    // Update the key/value pair.
                    selectedUser.CommandDictionary.Remove(commandPair.Key);

                    // Save to profile.
                    PublishUIData(saveToProfile: true);
                }

            }
        }

        #endregion

        #region Training

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

        // Take a picture of the active user and store it in the training images folder of the selected user.
        private void btnSnapshot_Click( object sender, RoutedEventArgs e )
        {
            int numFiles, maxFileNum;
            string pathUserTrainingFolder = Path.Combine(tboxTrainingImagesPath.Text, selectedUser.Guid.ToString());

            if ( AnalyzeTrainingImagesFolder(selectedUser, out numFiles, out maxFileNum) )
            {
                // Store a picture of the face provided by the latest frame data packet.
                if ( null != facePicture )
                {
                    facePicture.Save(Path.Combine(pathUserTrainingFolder, (maxFileNum + 1) + ".bmp"));
                    lblNumberSnapshots.Content = numFiles + 1;
                }
            }
        }

        private void btnFinish_Click( object sender, RoutedEventArgs e )
        {
            ToggleTrainingMode(false);

            PublishUIData(refreshTrainingImages: true);
        }

        #endregion

        private void cboxUserSelection_SelectionChanged( object sender = null, SelectionChangedEventArgs e = null )
        {
            // Update the binding of the Commands Listview.
            object selectedItem = cboxUserSelection.SelectedItem;
            if ( null != selectedItem )
            {
                SelectedUser = selectedItem as User;
            }
        }

        private void chkEnableTracking_Click( object sender, RoutedEventArgs e )
        {
            PublishUIData();
        }

        private void btnBrowse_Click( object sender, RoutedEventArgs e )
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

            // When the dialog is opened, have the folder that is currently referenced in the textbox be the folder
            // that is open in the dialog.
            dialog.SelectedPath = tboxTrainingImagesPath.Text;

            // Display the dialog.
            dialog.ShowDialog();

            // Update the textbox based on the folder that was selected.
            tboxTrainingImagesPath.Text = dialog.SelectedPath;

            PublishUIData(saveToProfile: true, refreshTrainingImages: true);
        }

        private void Window_Closing( object sender, CancelEventArgs e )
        {
            PublishUIData(saveToProfile: true, performCleanup: true);
        }


        #endregion

        // Retrieve the name of the latest picture to be saved in this folder.
        // The new file name should be incremented by one.
        private bool AnalyzeTrainingImagesFolder( User user, out int numFiles, out int nextFileNum )
        {
            // Initialize out variables.
            numFiles = 0;
            nextFileNum = 1;

            string pathUserTrainingFolder = Path.Combine(tboxTrainingImagesPath.Text, user.Guid.ToString());

            if ( Directory.Exists(pathUserTrainingFolder) )
            {
                List<FileInfo> files = (new DirectoryInfo(pathUserTrainingFolder)).GetFiles().ToList<FileInfo>();
                if ( 0 == files.Count )
                {
                    // Indicate success.
                    return true;
                }
                else
                {
                    try
                    {
                        numFiles = files.Count;
                        List<int> fileNameInts = files.Select(file => Convert.ToInt32(Path.GetFileNameWithoutExtension(file.Name))).ToList();
                        nextFileNum = fileNameInts.Max();

                        // Indicate success.
                        return true;
                    }
                    catch ( Exception ex )
                    {
                        MessageBox.Show("Training images folder has invalid files. Please clear this directory or select a new one.");
                    }
                }
            }

            return false;
        }

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

                // Update the label indicating the number of images that the user has.
                int numFiles, maxFileNum;
                AnalyzeTrainingImagesFolder(selectedUser, out numFiles, out maxFileNum);
                lblNumberSnapshots.Content = numFiles;
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
            // Populate the user collection.
            users = new ObservableCollection<User>(value.Users);

            // Update the user interface.
            chkEnableTracking.IsChecked = value.DrawDetectionRectangles;
            chkGreetUsers.IsChecked = value.HaveJarvisGreetUser;
            tboxTrainingImagesPath.Text = value.PathToTrainingImages;
            tboxZipCode.Text = value.ZipCode.ToString();
        }

        public void OnNext( FrameData value )
        {
            currentFrame = value.Frame;
            facePicture = value.Face;
            this.ActiveUser = value.ActiveUser;
        }

        public IDisposable Subscribe( IObserver<ConfigData> observer )
        {
            return SubscriptionManager.Subscribe(uiObservers, observer);
        }

        private void PublishUIData( bool saveToProfile = false, bool refreshTrainingImages = false, bool performCleanup = false )
        {
            ConfigData packet = new ConfigData();
            packet.DrawDetectionRectangles = chkEnableTracking.IsChecked ?? false;
            packet.HaveJarvisGreetUser = chkGreetUsers.IsChecked ?? false;
            packet.PathToTrainingImages = tboxTrainingImagesPath.Text;
            packet.ZipCode = zipCode;
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

        private void tboxZipCode_TextChanged( object sender, TextChangedEventArgs e )
        {
            TextBox tbox = sender as TextBox;
            string potentialZipCode = tbox.Text;
            zipCode = IsValidZipCode(potentialZipCode);
            if ( 0 != zipCode  )
            {
                PublishUIData();
            }
        }

        private int IsValidZipCode( string potentialZipCode )
        {
            if ( potentialZipCode.Length != 5 )
            {
                return 0;
            }

            try
            {
                int zipCode = Convert.ToInt32(potentialZipCode);

                return zipCode;
            }
            catch ( Exception ex)
            {
                return 0;
            }
        }

        private void chkGreetUsers_Click( object sender, RoutedEventArgs e )
        {
            PublishUIData();
        }
    }
}
