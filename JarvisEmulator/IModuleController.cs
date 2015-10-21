using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JarvisEmulator
{
    public interface IModuleController
    {
        BitmapSource QueryFrame();
    }
}
