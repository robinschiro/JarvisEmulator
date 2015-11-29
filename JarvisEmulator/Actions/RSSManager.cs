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
                    if ( count > 5 )
                        break;
                }
            }
            else
            {
                // Using the Yahoo API.
                //http://weather.yahooapis.com/forecastrss?p=84092

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(rssXmlDoc.NameTable);
                nsmgr.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");
                XmlNode xNode = rssXmlDoc.DocumentElement.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr);
                XmlAttributeCollection attrColl = rssXmlDoc.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr).Attributes;
                
                // Retrieve the conditions and temperature.
                XmlAttribute attr1 = attrColl["text"];
                string conditions = attr1.InnerXml;
                XmlAttribute attr2 = attrColl["temp"];
                string temperature = attr2.InnerXml;
                rssContent.Append("Today is " + conditions + " with a temperature of " + temperature + " degrees Fahrenheit ");
            }

            return rssContent.ToString();
        }
    }
}