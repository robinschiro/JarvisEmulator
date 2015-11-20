using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public class User
    {
        private string firstName;
        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }

        private string lastName;
        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
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

    }
}
