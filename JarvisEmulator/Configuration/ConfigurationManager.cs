using System;
using System.Collections.Generic;
using tvToolbox;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Windows;
using System.IO;

namespace JarvisEmulator
{
    public struct ConfigData
    {
        public bool DrawDetectionRectangles;
        public bool HaveJarvisGreetUser;
        public List<User> Users;
        public string PathToTrainingImages;

        public bool IsInit;
    }

    public class ConfigurationManager : IObservable<ConfigData>, IObserver<UIData>
    {
        #region Configuration Members

        private tvProfile profile;
        private User activeUser;
        private List<User> users = new List<User>();
        private string pathToTrainingImages;
        private bool haveJarvisGreetUsers;
        private bool drawDetectionRectangles;

        #endregion

        #region Observer Lists

        List<IObserver<ConfigData>> configObservers = new List<IObserver<ConfigData>>();

        #endregion

        public ConfigurationManager()
        {

        }

        private void SaveToProfile( UIData data )
        {
            try
            {
                profile.ClearFileContents();

                // Update the profile file with data from the UIData packet.
                profile.Add("-TrainingImagesFolder", data.PathToTrainingImages);
                profile.Add("-HaveJarvisGreetUsers", data.HaveJarvisGreetUser);
                profile.Add("-DrawDetectionRectangles", data.DrawDetectionRectangles);

                // Create a "User" profile for each user in the list.
                foreach ( User user in data.Users )
                {
                    tvProfile userProfile = new tvProfile();
                    userProfile.Add("-Guid", user.Guid);
                    userProfile.Add("-FirstName", user.FirstName);
                    userProfile.Add("-LastName", user.LastName);

                    // Create a "CommandPair" profile for each command pair.
                    foreach ( KeyValuePair<string, string> commandPair in user.CommandDictionary )
                    {
                        tvProfile commandPairProfile = new tvProfile();
                        commandPairProfile.Add("-CommandKey", commandPair.Key);
                        commandPairProfile.Add("-CommandValue", commandPair.Value);

                        userProfile.Add("-CommandPair", commandPairProfile);
                    }

                    profile.Add("-User", userProfile);
                }

                // Save the profile.
                profile.Save();
            }
            catch ( Exception ex )
            {
                MessageBox.Show("Settings were not saved", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Create a list of users using the data in the configuration file.
        public void ParseProfile()
        {
            // Temporary variables used for creating each user instance.
            string firstName;
            string lastName;
            Guid guid;
            ObservableDictionary<string, string> commandDictionary;

            // Scan the existing profile.
            profile = new tvProfile(tvProfileDefaultFileActions.AutoLoadSaveDefaultFile, tvProfileFileCreateActions.NoPromptCreateFile);

            // Retrieve the path to the training images folder.
            pathToTrainingImages = profile.sValue("-TrainingImagesFolder", Path.Combine(Directory.GetCurrentDirectory(), "TrainingImages"));

            // Determine if the application should greet users upon entrance.
            haveJarvisGreetUsers = profile.bValue("-HaveJarvisGreetUsers", false);

            drawDetectionRectangles = profile.bValue("-DrawDetectionRectangles", false);

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
                commandDictionary = new ObservableDictionary<string, string>();
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
            data.DrawDetectionRectangles = drawDetectionRectangles;
            data.HaveJarvisGreetUser = haveJarvisGreetUsers;
            data.PathToTrainingImages = pathToTrainingImages;
            data.Users = users;
            data.IsInit = true;
            

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

        public void OnNext( UIData value )
        {
            if ( value.SaveToProfile )
            {
                SaveToProfile(value);
            }
        }

        public IDisposable Subscribe( IObserver<ConfigData> observer )
        {
            return SubscriptionManager.Subscribe(configObservers, observer);
        }
    }
}
