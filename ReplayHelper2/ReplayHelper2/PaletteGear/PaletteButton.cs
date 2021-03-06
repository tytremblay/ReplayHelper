﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    public class PaletteButton
    {
        public event EventHandler Updated;

        private bool WasPressed = false;

        public bool IsPressed { get; private set; } = false;

        public bool JustPressed
        {
            get
            {
                return IsPressed && !WasPressed;
            }
        }

        public bool JustReleased
        {
            get
            {
                return !IsPressed && WasPressed;
            }
        }

        public PaletteButton()
        {
        }

        public void Update(bool pressed)
        {
            WasPressed = IsPressed;
            IsPressed = pressed;
            Updated?.Invoke(this, new EventArgs());
        }

        public void Update(int[] moduleData)
        {
            Update(moduleData[0] == 1);
        }
    }
}
