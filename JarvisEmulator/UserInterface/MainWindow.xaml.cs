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

namespace JarvisEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModuleController controller;

        public MainWindow()
        {
            // Create the user interface.
            InitializeComponent();

            // Create the ModuleController.
            controller = new ModuleController(this);
        }

        #region Button Events

        private void btnDisplayFeed_Click( object sender, RoutedEventArgs e )
        {
            // During Application Idle (perhaps run this on a separate thread at some point),
            // update the video feed with frames from the FaceDetector.
            imgVideoFeed.Source = controller.GetCurrentFrame();
        }

        private void BrowseForFile( object sender, RoutedEventArgs e )
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();

            tboxTGitPath.Text = dialog.FileName;
        }

        #endregion
    }
}
