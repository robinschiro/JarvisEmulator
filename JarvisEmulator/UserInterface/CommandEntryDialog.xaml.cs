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
    /// Interaction logic for CommandEntryDialog.xaml
    /// </summary>
    public partial class CommandEntryDialog : Window
    {
        private string commandKey;
        public string CommandKey
        {
            get { return commandKey; }
            set { commandKey = value;  }
        }

        private string commandValue;
        public string CommandValue
        {
            get { return commandValue; }
            set { commandValue = value; }
        }

        public CommandEntryDialog( string commandKey = "", string commandValue = "" )
        {
            InitializeComponent();

            CommandKey = commandKey;
            CommandValue = commandValue;

            // If either the key or the value was passed it, this must be an entry being modified.
            // Make the key textbox read only.
            if ( !(String.IsNullOrEmpty(commandKey) && String.IsNullOrEmpty(commandValue)) )
            {
                tboxCommandKey.IsEnabled = false;
            }

            this.DataContext = this;
        }

        private void btnOk_Click( object sender, RoutedEventArgs e )
        {
            this.Close();
        }
    }
}
