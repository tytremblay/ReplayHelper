using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    class PaletteDataEventArgs : EventArgs
    {
        private int module;
        private int[] data;

        public PaletteDataEventArgs(PaletteData paletteData)
        {
            module = paletteData.i;
            data = paletteData.v;
        }

        public int ModuleNumber => module;
        public int[] Data => data;
    }
}
