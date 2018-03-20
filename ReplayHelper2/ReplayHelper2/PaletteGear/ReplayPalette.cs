using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    class ReplayPalette : PaletteGear
    {
        public double playbackSpeed => getPlaybackSpeed();
        public int scrub => axes[1];
        public PaletteButton PlayPauseButton => buttons[0];
        public PaletteButton ClearButton => buttons[1];
        public PaletteButton OpenButton => buttons[2];

        public ReplayPalette(Joystick stick) : base(stick)
        {
        }

        public double getPlaybackSpeed()
        {
            switch (axes[0])
            {
                case var v when (v >= 75):
                    return 4.0;
                case var v when (v >= 50):
                    return 3.0;
                case var v when (v >= 25):
                    return 2.0;
                case var v when (v > -25):
                    return 1.0;
                case var v when (v <= -25 & v > -50):
                    return 0.5;
                case var v when (v <= -50 & v > -75):
                    return 0.25;
                case var v when (v <= -75):
                    return 0.125;
                default:
                    return 1.0;
            }
        }

        public override string ToString()
        {
            return $"PlaybackSpeed: {playbackSpeed}, Scrub: {scrub}, playPause: {PlayPauseButton.IsPressed}, clearDrawings: {ClearButton.IsPressed}; openRecent: {OpenButton.IsPressed}";
        }
    }
}
