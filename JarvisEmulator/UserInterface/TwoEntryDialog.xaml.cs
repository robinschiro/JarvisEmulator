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
using System.Windows.Shapes;

namespace JarvisEmulator
{
    /// <summary>
    /// Interaction logic for TwoEntryDialog.xaml
    /// </summary>
    public partial class TwoEntryDialog : Window
    {
        private string entryOne;
        public string EntryOne
        {
            get { return entryOne; }
            set { entryOne = value; }
        }

        private string entryTwo;
        public string EntryTwo
        {
            get { return entryTwo; }
            set { entryTwo = value; }
        }

        private bool result;
        public bool Result
        {
            get { return result; }
            set { result = value; }
        }

        public TwoEntryDialog( string title, string entryOneLabel, string entryTwoLabel, string defaultEntryOne = "", string defaultEntryTwo = "" )
        {
            InitializeComponent();

            // Set labels.
            lblTitle.Content = title;
            lblEntryOne.Content = entryOneLabel;
            lblEntryTwo.Content = entryTwoLabel;

            // Update values in the textboxes.
            this.EntryOne = defaultEntryOne;
            this.EntryTwo = defaultEntryTwo;

            this.DataContext = this;
        }

        private void btnOk_Click( object sender, RoutedEventArgs e )
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            this.Result = !(String.IsNullOrEmpty(entryOne) || String.IsNullOrEmpty(entryTwo));
            this.Close();
        }

        private void Window_KeyDown( object sender, KeyEventArgs e )
        {
            if ( Key.Enter == e.Key )
            {
                CloseWindow();
            }
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            tboxEntryOne.Focus();
        }
    }
}
