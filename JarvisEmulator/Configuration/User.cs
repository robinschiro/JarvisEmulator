using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public class User
    {
        private string firstName;
        private string lastName;

        // Used to uniquely identify the user.
        private Guid guid;
        public Guid Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        // Used to map user commands to either a) specfic URLS or b) specific applications.
        private Dictionary<string, string> commandDictionary;
        public Dictionary<string, string> CommandDictionary
        {
            get
            {
                return commandDictionary;
            }
            set
            {
                commandDictionary = value;
            }
        }

        public User( Guid guid, string firstName, string lastName, Dictionary<string, string> commandDictionary )
        {
            this.guid = guid;
            this.firstName = firstName;
            this.lastName = lastName;
            this.commandDictionary = commandDictionary;
        }

    }
}
