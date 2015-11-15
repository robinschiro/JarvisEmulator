using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public struct ActionData
    {
        public string Message;
    }

    public class ActionManager : IObservable<ActionData>
    {
        RSSManager rssManager;

        public ActionManager()
        {
            rssManager = new RSSManager();
        }

        public IDisposable Subscribe( IObserver<ActionData> observer )
        {
            throw new NotImplementedException();
        }
    }
}
