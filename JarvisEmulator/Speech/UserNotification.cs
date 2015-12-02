// ===============================
// AUTHOR: Manuel Gonzalez
// PURPOSE: To provide a means by which user notifications can be packaged.
// ===============================
// Change History:
//
// MG   11/15/2015  Created class
//
//==================================

namespace JarvisEmulator
{
    public enum NOTIFICATION_TYPE
    {
        RSS_DATA, WARNING, ERROR, USER_ENTERED, OPENING_APPLICATION, ALREADY_OPENED, NO_APP_TO_CLOSE, CLOSING_APPLICATION, LOG_OUT
    };

    public class UserNotification
    {
        public NOTIFICATION_TYPE type;
        public string data;
        public string userName;

        public UserNotification( NOTIFICATION_TYPE type, string userName, string data = "")
        {
            this.type = type;
            this.userName = userName;
            this.data = data;
        }
    }
}