using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ReplayHelper
{
    class PaletteGear
    {
        private async void ReadWriteToHidDevice(HidDevice device)
        {
            if (device != null)
            {
                // construct a HID output report to send to the device
                HidOutputReport outReport = device.CreateOutputReport();

                /// Initialize the data buffer and fill it in
                byte[] buffer = new byte[] { 10, 20, 30, 40 };

                DataWriter dataWriter = new DataWriter();
                dataWriter.WriteBytes(buffer);

                outReport.Data = dataWriter.DetachBuffer();

                // Send the output report asynchronously
                await device.SendOutputReportAsync(outReport);

                //
                // Sent output report successfully 
                // Now lets try read an input report 
                //
                HidInputReport inReport = await device.GetInputReportAsync();

                if (inReport != null)
                {
                    UInt16 id = inReport.Id;
                    var bytes = new byte[4];
                    DataReader dataReader = DataReader.FromBuffer(inReport.Data);
                    dataReader.ReadBytes(bytes);
                }
                else
                {
                    Debug.WriteLine("Invalid input report received");
                }
            }
            else
            {
                Debug.WriteLine("device is NULL");
            }
        }
    }
}
