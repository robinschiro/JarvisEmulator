using System;
using System.Collections.Generic;
using tvToolbox;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace JarvisEmulator
{
    public struct ConfigData
    {
        public bool DrawDetectionRectangles;
        public bool HaveJarvisGreetUser;
        public List<User> Users;
        public User ActiveUser;
        public string PathToTrainingImages;
    }

    public class ConfigurationManager : IObservable<ConfigData>, IObserver<User>, IObserver<UIData>
    {
        #region Configuration Members

        private tvProfile profile;
        private User activeUser;
        private List<User> users = new List<User>();
        private string pathToTrainingImages;
        private bool haveJarvisGreetUsers;

        #endregion

        #region Observer Lists

        List<IObserver<ConfigData>> configObservers = new List<IObserver<ConfigData>>();

        #endregion

        public ConfigurationManager()
        {

        }

        private void UpdateProfile( UIData data )
        {

        }

        // Create a list of users using the data in the configuration file.
        public void ParseProfile()
        {
            // Temporary variables used for creating each user instance.
            string firstName;
            string lastName;
            Guid guid;
            Dictionary<string, string> commandDictionary;

            // Scan the existing profile.
            profile = new tvProfile(tvProfileDefaultFileActions.AutoLoadSaveDefaultFile, tvProfileFileCreateActions.NoPromptCreateFile);

            // Retrieve the path to the training images folder.
            pathToTrainingImages = profile.sValue("-TrainingImagesFolder", "");

            // Determine if the application should greet users upon entrance.
            haveJarvisGreetUsers = profile.bValue("-HaveJarvisGreetUsers", false);

            // Retrieve a profile of all the users.
            tvProfile userProfiles = profile.oOneKeyProfile("-User");

            // Itereate through the user profile to populate the list of users.
            foreach ( DictionaryEntry userEntry in userProfiles )
            {
                // Retrieve this user's profile. 
                tvProfile userProfile = new tvProfile(userEntry.Value.ToString());

                // Parse the profile.
                guid = new Guid(userProfile.sValue("-Guid", ""));
                firstName = userProfile.sValue("-FirstName", "");
                lastName = userProfile.sValue("-LastName", "");

                // Create the command dictionary.
                tvProfile commandPairProfiles = userProfile.oOneKeyProfile("-CommandPair");
                commandDictionary = new Dictionary<string, string>();
                foreach ( DictionaryEntry commandPairEntry in commandPairProfiles )
                {
                    // Retrieve this command pair profile. 
                    tvProfile commandPairProfile = new tvProfile(commandPairEntry.Value.ToString());
                    commandDictionary.Add(commandPairProfile.sValue("-CommandKey", ""), commandPairProfile.sValue("-CommandValue", ""));
                }

                // Add the user to the list.
                users.Add(new User(guid, firstName, lastName, commandDictionary));
            }

            // Create a config data packet.
            ConfigData data = new ConfigData();
            data.DrawDetectionRectangles = profile.bValue("-DrawDetectionRectangles", false);
            data.HaveJarvisGreetUser = haveJarvisGreetUsers;
            data.PathToTrainingImages = pathToTrainingImages;
            data.Users = users;

            // Send configuration information to the observers.
            SubscriptionManager.Publish(configObservers, data);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnNext( User value )
        {
            activeUser = value;
        }

        public void OnNext( UIData value )
        {
        }

        public IDisposable Subscribe( IObserver<ConfigData> observer )
        {
            return SubscriptionManager.Subscribe(configObservers, observer);
        }
    }
}
