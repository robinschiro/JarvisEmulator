// ===============================
// AUTHOR: Robin Schiro
// PURPOSE: To store information about a specific user.
// ===============================
// Change History:
//
// RS   10/20/2015  Created class
//
//==================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public class User : INotifyPropertyChanged
    {
        private string firstName;
        public string FirstName
        {
            get { return firstName; }
            set
            {
                firstName = value;
                NotifyPropertyChanged("FirstName");
            }
        }

        private string lastName;
        public string LastName
        {
            get { return lastName; }
            set
            {
                lastName = value;
                NotifyPropertyChanged("LastName");
            }
        }

        // Used to uniquely identify the user.
        private Guid guid;
        public Guid Guid
        {
            get { return guid; }
            set { guid = value; }
        }

        // Used to map user commands to either a) specfic URLS or b) specific applications.
        private ObservableDictionary<string, string> commandDictionary;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableDictionary<string, string> CommandDictionary
        {
            get { return commandDictionary; }
            set { commandDictionary = value; }
        }

        public User( Guid guid, string firstName, string lastName, ObservableDictionary<string, string> commandDictionary )
        {
            this.guid = guid;
            this.firstName = firstName;
            this.lastName = lastName;
            this.commandDictionary = commandDictionary;
        }

        public override string ToString()
        {
            return firstName + " " + lastName;
        }

        private void NotifyPropertyChanged( string propertyName = "" )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
