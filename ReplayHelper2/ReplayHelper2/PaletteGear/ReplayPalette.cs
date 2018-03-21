using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHelper2.PaletteGear
{
    class ReplayPalette
    {
        private PaletteBase PaletteBase;
        public PaletteSlider SpeedSlider { get; private set; } = new PaletteSlider();
        public PaletteDial ScrubDial { get; private set; } = new PaletteDial();
        public PaletteButton ClearButton { get; private set; } = new PaletteButton();
        public PaletteButton OpenButton { get; private set; } = new PaletteButton();

        private enum Module
        {
            Base = 1, Open = 2, Speed = 3, Scrub = 4, Clear = 5
        }

        public ReplayPalette(string comPort)
        {
            PaletteBase = new PaletteBase(comPort);
            PaletteBase.DataRecieved += OnDataRecieved;
        }

        private void OnDataRecieved(object sender, PaletteDataEventArgs e)
        {
            Module module = (Module)e.ModuleNumber;

            switch (module)
            {
                case Module.Base:
                    break;
                case Module.Open:
                    OpenButton.Update(e.Data);
                    break;
                case Module.Speed:
                    SpeedSlider.Update(e.Data);
                    break;
                case Module.Scrub:
                    ScrubDial.Update(e.Data);
                    break;
                case Module.Clear:
                    ClearButton.Update(e.Data);
                    break;
            }
        }

        public double getPlaybackSpeed()
        {
            switch (SpeedSlider.Value)
            {
                case var v when (v >= 236):
                    return 4.0;
                case var v when (v >= 200):
                    return 3.0;
                case var v when (v >= 144):
                    return 2.0;
                case var v when (v >= 108):
                    return 1.0;
                case var v when (v >= 72):
                    return 0.5;
                case var v when (v >= 36):
                    return 0.25;
                case var v when (v >= 0):
                    return 0.125;
                default:
                    return 1.0;
            }
        }

        public override string ToString()
        {
            return $"PlaybackSpeed: {getPlaybackSpeed()}, Scrub: {ScrubDial.Speed}, playPause: {ScrubDial.Button.IsPressed}, clearDrawings: {ClearButton.IsPressed}; openRecent: {OpenButton.IsPressed}";
        }
    }
}
