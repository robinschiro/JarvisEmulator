using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace JarvisEmulator
{
    public struct RSSData
    {
        public string parsedString;
    }


    public class RSSManager : IObservable<RSSData>, IObserver<ActionData>
    {
        string url;

        private List<IObserver<RSSData>> RSSObservers = new List<IObserver<RSSData>>();
    
   
        private void PublishRSSString()
        {
            RSSData info = new RSSData();

            info.parsedString = parseRss(url);
            SubscriptionManager.Publish(RSSObservers, info);
        }

        public void OnCompleted()
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
        }

        public IDisposable Subscribe( IObserver<RSSData> observer )
        {
           
            return SubscriptionManager.Subscribe(RSSObservers, observer);
        }

        public string parseRss( string url )
        {

            XmlDocument rssXmlDoc = new XmlDocument();
            rssXmlDoc.Load(url);
            XmlNodeList rssNodes = rssXmlDoc.SelectNodes("rss/channel/item");
            StringBuilder rssContent = new StringBuilder();


            if ( url.Contains("news") || url.Contains("reddit") )
            {
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

                    rssContent.Append(x + "." + title + "   ");
                    x++;
                    if ( count > 5 )
                        break;
                }
            }
            else
            {
                foreach ( XmlNode rssNode in rssNodes )
                {
                    XmlNode rssSubNode = rssNode.SelectSingleNode("title");
                    string title = rssSubNode != null ? rssSubNode.InnerText : "";

                    //http://weather.yahooapis.com/forecastrss?p=32816

                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(url);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xdoc.NameTable);
                    nsmgr.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");
                    XmlNode xNode = xdoc.DocumentElement.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr);
                    XmlAttributeCollection attrColl = xdoc.SelectSingleNode("/rss/channel/item/yweather:condition", nsmgr).Attributes;


                    XmlAttribute attr1 = attrColl["text"];
                    string conditions = attr1.InnerXml;
                    XmlAttribute attr2 = attrColl["temp"];
                    string temperature = attr2.InnerXml;
                    rssContent.Append(title + " Today is " + conditions + " with a temperature of " + temperature + " degrees Ferinheight ");

                }
            }
            return rssContent.ToString();
        }
    }
}