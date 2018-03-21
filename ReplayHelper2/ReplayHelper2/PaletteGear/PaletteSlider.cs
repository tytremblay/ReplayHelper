using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    class PaletteSlider
    {
        public event EventHandler Updated;
        public int Value { get; private set; }

        public void Update(int[] data)
        {
            //{"in":[{"i":3,"v":[14,0,0,0,0,0,0,0]}]}
            this.Value = data[0];
            Updated?.Invoke(this, new EventArgs());
        }
    }
}
