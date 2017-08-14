using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace Sensorhub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UdpClient receivingUdpClientMyo;
        Thread myoThread;
        bool myoRunning = false;
        int myoPort = 11001;

        public MainWindow()
        {
            InitializeComponent();
            receivingUdpClientMyo = new UdpClient(myoPort);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process.Start(path+ "/runables/socketTest.exe");
                myoRunning = true;
                myoThread = new Thread(new ThreadStart(myoRunningThread));
                myoThread.Start();
            }
           catch (Exception xx)
            {
                int x = 1;
                x++;
            }
        }

      

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                myoRunning = false;
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process[] pp= System.Diagnostics.Process.GetProcesses();
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName("socketTest");
                pp1[0].CloseMainWindow();

            }
            catch (Exception xx)
            {
                int x = 1;
                x++;
            }
        }



        private void myoRunningThread()
        {
            while (myoRunning == true)
            {
                //Creates an IPEndPoint to record the IP Address and port number of the sender. 
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = receivingUdpClientMyo.Receive(ref RemoteIpEndPoint);

                    string returnData = Encoding.ASCII.GetString(receiveBytes);

                    Console.WriteLine("This is the message you received " +
                                                 returnData.ToString());
                    Console.WriteLine("This message was sent from " +
                                                RemoteIpEndPoint.Address.ToString() +
                                                " on their port number " +
                                                RemoteIpEndPoint.Port.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

    }
}
