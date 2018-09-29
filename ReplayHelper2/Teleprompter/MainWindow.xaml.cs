using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Teleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ServerIPBox.Text = Properties.Settings.Default.ServerIpAddress;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            sendMessage(TeleprompterMessageBox.Text, ServerIPBox.Text, 40190);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            sendMessage(" ", ServerIPBox.Text, 40190);
        }

        private void sendMessage(string message, string ipAddress, int port)
        {
            //---create a TCPClient object at the IP and port no.---
            TcpClient client = new TcpClient(ipAddress, port);
            NetworkStream nwStream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
        }

        private void SaveIpButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ServerIpAddress = ServerIPBox.Text;
            Properties.Settings.Default.Save();
        }
    }
}
