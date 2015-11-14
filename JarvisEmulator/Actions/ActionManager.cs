using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisEmulator
{
    public class ActionManager
    {
        public static void CommandLogout()
        {

            System.Diagnostics.Process.Start("shutdown", "-l");
        }

    }


}
