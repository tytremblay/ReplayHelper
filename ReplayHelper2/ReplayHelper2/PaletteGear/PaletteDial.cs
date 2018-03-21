using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    class PaletteDial
    {
        public event EventHandler Updated;
        public int Value { get; private set; } = 0;
        public int Speed { get; private set; } = 0;
        public PaletteButton Button { get; private set; } = new PaletteButton();

        public void Update(int[] data)
        {
            //{"in":[{"i":3,"v":[14,0,0,0,0,0,0,0]}]}
            this.Value = data[3];
            int leftSpeed = data[1];
            int rightSpeed = data[2];
            this.Speed = leftSpeed != 0 ? -leftSpeed : rightSpeed;
            this.Button.Update(data[0] == 1);
            Updated?.Invoke(this, new EventArgs());
        }
    }
}
