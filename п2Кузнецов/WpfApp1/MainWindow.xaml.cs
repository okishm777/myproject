using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool done = true;
        private UdpClient client;
        private IPAddress groupAddress;
        private int localPort;
        private int remotePort;
        private int ttl;

        private IPEndPoint remoteEP;
        private UnicodeEncoding encoding = new UnicodeEncoding();

        private string name;
        private string message;

        private readonly SynchronizationContext _syncContext;
        public MainWindow()

        {
            InitializeComponent();
            try
            {
                NameValueCollection configuration = ConfigurationSettings.AppSettings;
                groupAddress = IPAddress.Parse(configuration["GroupAddress"]);
                localPort = int.Parse(configuration["LocalPort"]);
                remotePort = int.Parse(configuration["RemotePort"]);
                ttl = int.Parse(configuration["TTL"]);
            }
            catch
            {
                MessageBox.Show(this, "Error in application configuration file!",
                "Error Multicast Chart",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _syncContext = SynchronizationContext.Current;
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            name = textName.Text;
            textName.IsReadOnly = true;

            try
            {
                client = new UdpClient(localPort);
                client.JoinMulticastGroup(groupAddress, ttl);

                remoteEP = new IPEndPoint(groupAddress, remotePort);
                Thread receiver = new Thread(new ThreadStart(Listener));
                receiver.IsBackground = true;
                receiver.Start();

                byte[] data = encoding.GetBytes(name + "has joined the chat");
                client.Send(data, data.Length, remoteEP);

                buttonStart.IsEnabled = false;
                buttonStop.IsEnabled = true;
                buttonSend.IsEnabled = true;
            }
            catch (SocketException ex)
            {
                MessageBox.Show(this, ex.Message, "Error MulticastChart",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Listener()
        {
            done = false;

            try
            {
                while (!done)
                {
                    IPEndPoint ep = null;
                    byte[] buffer = client.Receive(ref ep);
                    message = encoding.GetString(buffer);

                    _syncContext.Post(o => DisplayRecivedMessage(), null);
                }
            }
            catch (Exception ex)
            {
                if (done)
                    return;
                else
                    MessageBox.Show(this, ex.Message, "Error MulticastChat",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DisplayRecivedMessage()
        {
            string time = DateTime.Now.ToString("t");
            textMessages.Text = time + " " + message + "\r\n" + textMessages.Text;
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] data = encoding.GetBytes(name + " : " + textMessage.Text);
                client.Send(data, data.Length, remoteEP);
                textMessage.Clear();
                textMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error MulticastChat",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            StopListener();
        }

        private void StopListener()
        {
            byte[] data = encoding.GetBytes(name + "has left the chart");
            client.Send(data, data.Length, remoteEP);

            client.DropMulticastGroup(groupAddress);
            client.Close();

            done = true;

            buttonStart.IsEnabled = true;
            buttonStop.IsEnabled = false;
            buttonSend.IsEnabled = false;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!done)
                StopListener();
        }
    }
}