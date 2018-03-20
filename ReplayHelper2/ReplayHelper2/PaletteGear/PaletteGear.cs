using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.DirectInput;

namespace ReplayHelper2.PaletteGear
{
    class PaletteGear
    {
        public Joystick joystick { get; set; } = null;
        public int[] axes { get; } = new int[8];
        public PaletteButton[] buttons { get; } = new PaletteButton[24];

        public PaletteGear(Joystick stick)
        {
            this.joystick = stick;
            for (int i = 0; i < 24; i++)
            {
                buttons[i] = new PaletteButton();
            }
        }

        public void Refresh()
        {
            JoystickState state = joystick.GetCurrentState();
            axes[0] = state.X;
            axes[1] = state.Y;
            axes[2] = state.Z;
            axes[3] = state.RotationX;
            axes[4] = state.RotationY;
            axes[5] = state.RotationZ;


            for (int i = 0; i < 24; i++)
            {
                buttons[i].Update(state.GetButtons()[i]);
            }
        }
    }
}
