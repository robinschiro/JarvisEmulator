// ===============================
// AUTHOR: Julian Rojas
// PURPOSE: To retrieve and parse information from a given rss feed.
// ===============================
// Change History:
//
// JR   10/20/2015  Created class
//
//==================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace JarvisEmulator
{
    public struct RSSData
    {
        public string parsedString;
    }


    public class RSSManager// : IObservable<RSSData>, IObserver<ActionData>
    {
        // Dictionary for state abbreviations.
        private Dictionary<string, string> stateAbbreviations = new Dictionary<string, string>()
        {
            {"AL", "ALABAMA"},
            {"AK", "ALASKA"},
            {"AS", "AMERICAN SAMOA"},
            {"AZ", "ARIZONA"},
            {"AR", "ARKANSAS"},
            {"CA", "CALIFORNIA"},
            {"CO", "COLORADO"},
            {"CT", "CONNECTICUT"},
            {"DE", "DELAWARE"},
            {"DC", "DISTRICT OF COLUMBIA"},
            {"FM", "FEDERATED STATES OF MICRONESIA"},
            {"FL", "FLORIDA"},
            {"GA", "GEORGIA"},
            {"GU", "GUAM GU"},
            {"HI", "HAWAII"},
            {"ID", "IDAHO"},
            {"IL", "ILLINOIS"},
            {"IN", "INDIANA"},
            {"IA", "IOWA"},
            {"KS", "KANSAS"},
            {"KY", "KENTUCKY"},
            {"LA", "LOUISIANA"},
            {"ME", "MAINE"},
            {"MH", "MARSHALL ISLANDS"},
            {"MD", "MARYLAND"},
            {"MA", "MASSACHUSETTS"},
            {"MI", "MICHIGAN"},
            {"MN", "MINNESOTA"},
            {"MS", "MISSISSIPPI"},
            {"MO", "MISSOURI"},
            {"MT", "MONTANA"},
            {"NE", "NEBRASKA"},
            {"NV", "NEVADA"},
            {"NH", "NEW HAMPSHIRE"},
            {"NJ", "NEW JERSEY"},
            {"NM", "NEW MEXICO"},
            {"NY", "NEW YORK"},
            {"NC", "NORTH CAROLINA"},
            {"ND", "NORTH DAKOTA"},
            {"MP", "NORTHERN MARIANA ISLANDS"},
            {"OH", "OHIO"},
            {"OK", "OKLAHOMA"},
            {"OR", "OREGON"},
            {"PW", "PALAU"},
            {"PA", "PENNSYLVANIA"},
            {"PR", "PUERTO RICO"},
            {"RI", "RHODE ISLAND"},
            {"SC", "SOUTH CAROLINA"},
            {"SD", "SOUTH DAKOTA"},
            {"TN", "TENNESSEE"},
            {"TX", "TEXAS"},
            {"UT", "UTAH"},
            {"VT", "VERMONT"},
            {"VI", "VIRGIN ISLANDS"},
            {"VA", "VIRGINIA"},
            {"WA", "WASHINGTON"},
            {"WV", "WEST VIRGINIA"},
            {"WI", "WISCONSIN"},
            {"WY", "WYOMING"},
        };

        // A url must be provided before calling the PublishRSSString function
        private string url;
        public string URL
        {
            get { return url; }
            set { url = value; }
        }

        private string nickname;
        public string NickName
        {
            get { return nickname; }
            set { nickname = value; }
        }

        // Give a function that calls back when the output is ready when creating the rss manager
        public RSSManager ( Func<RSSData, bool> actionManagerOutFunction )
        {
            this.actionManagerOutFunction = actionManagerOutFunction;
        }

        Func<RSSData, bool> actionManagerOutFunction = null; 

        //private List<IObserver<RSSData>> RSSObservers = new List<IObserver<RSSData>>();


        public void PublishRSSString()
        {
            // If no url is given. Stop inmediatelly
            if (url == "")
                return;

            RSSData info = new RSSData();

            info.parsedString = parseRss(url);

            // If the action manager has given you the bypassing function, just call it with the resulting info
            //  The action manager will take it from here and send it to whoever needs it
            if( actionManagerOutFunction != null )
                actionManagerOutFunction(info);

            url = "";
        }

        /*public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnNext( ActionData value )
        {
          if (value.inMessage.Contains("rss"))
            {
                url = value.inMessage;
            }
        }*/

        /*public IDisposable Subscribe( IObserver<RSSData> observer )
        {
           
            return SubscriptionManager.Subscribe(RSSObservers, observer);
        }*/

        private string parseRss( string url )
        {
            XmlDocument rssXmlDoc = new XmlDocument();

            // Attempt to load the URL.
            // If this fails, return an error message.
            try
            {
                rssXmlDoc.Load(url);
            }
            catch ( Exception ex )
            {
                return "Failed to retrieve data from " + nickname;
            }
            StringBuilder rssContent = new StringBuilder();

            if ( !url.ToLower().Contains("weather") )
            {
                XmlNodeList rssNodes = rssXmlDoc.SelectNodes("rss/channel/item");

                int count = 0;
                int x = 1;
                foreach ( XmlNode rssNode in rssNodes )
                {
                    XmlNode rssSubNode = rssNode.SelectSingleNode("title");
                    string title = rssSubNode != null ? rssSubNode.InnerText : "";

                    rssSubNode = rssNode.SelectSingleNode("link");
                    string link = rssSubNode != null ? rssSubNode.InnerText : "";

                    rssSubNode = rssNode.SelectSingleNode("description");
                    string description = rssSubNode != null ? rssSubNode.InnerText : "";

                    count++;

                    rssContent.Append(x + " " + title + "   ");
                    x++;
                    if ( count > 4 )
                        break;
                }
            }
            else
            {
                // Using the Yahoo API.
                // http://weather.yahooapis.com/forecastrss?p=84092

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(rssXmlDoc.NameTable);
                nsmgr.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");

                XmlNode titleNode = rssXmlDoc.DocumentElement.SelectSingleNode("/rss/channel/title", nsmgr);
                string cityAndState = titleNode != null ? titleNode.InnerText : "";
                string city = cityAndState.Substring(cityAndState.LastIndexOf('-') + 2, cityAndState.LastIndexOf(',') - cityAndState.LastIndexOf('-') - 2);
                string stateAbbrev = cityAndState.Substring(cityAndState.LastIndexOf(", ") + 2);
                string state = stateAbbreviations[stateAbbrev];


                XmlNode xNode = rssXmlDoc.DocumentElement.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr);
                XmlAttributeCollection attrColl = rssXmlDoc.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr).Attributes;
                
                // Retrieve the conditions and temperature.
                XmlAttribute attr1 = attrColl["text"];
                string conditions = attr1.InnerXml;
                XmlAttribute attr2 = attrColl["temp"];
                string temperature = attr2.InnerXml;
                rssContent.Append("Today is " + conditions + " with a temperature of " + temperature + " degrees Fahrenheit in " + city + ", " + state);
            }

            return rssContent.ToString();
        }
    }
}