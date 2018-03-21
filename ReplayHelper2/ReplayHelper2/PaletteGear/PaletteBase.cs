using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Newtonsoft.Json;

namespace ReplayHelper2.PaletteGear
{
    class PaletteBase
    {
        public event EventHandler<PaletteDataEventArgs> DataRecieved;

        private SerialPort _port;

        public PaletteBase(string comPort)
        {
            _port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
            _port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            //_port.Open();
        }
        
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //{"in":[{"i":3,"v":[14,0,0,0,0,0,0,0]}]}
            string data = _port.ReadExisting();
            try
            {
                PaletteDataContainer paletteData = JsonConvert.DeserializeObject<PaletteDataContainer>(data);
                PaletteDataEventArgs paletteDataEventArgs = new PaletteDataEventArgs(paletteData.@in[0]);
                DataRecieved?.Invoke(this, paletteDataEventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
