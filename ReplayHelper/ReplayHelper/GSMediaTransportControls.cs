using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ReplayHelper
{
    class GSMediaTransportControls : MediaTransportControls
    {
        public event EventHandler<EventArgs> RecordClicked;

        public GSMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(GSMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            // Find the custom button and create an event handler for its Click event. 
            var recordButton = GetTemplateChild("RecordButton") as Button;
            recordButton.Click += RecordButton_Click;
            base.OnApplyTemplate();
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'like' is clicked. 
            var handler = RecordClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
