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
        //sockets that receive messages from the different applications
        UdpClient receivingUdpClientMyo;
        UdpClient receivingUdpClientPen;
        UdpClient receivingUdpClientTest;

        Thread storingThread;
        bool isStoring = false;

        //Threads that receive messages from the different applications
        Thread testThread;
        Thread penThread;
        Thread myoThread;
        bool myoRunning = false;
        bool penRunning = false;
        bool testRunning = false;

        //ports from the different applications
        int penPort = 11003;
        int myoPort = 11002;
        int testPort = 11001;

        //strings for the files of the different applications
        string penStringFile = "penApp";
        string testStringFile = "socketTest";
        string myoStringFile = "MyoApp";

        //strings for the different applications
        string penString = "";
        string myoString = "";
        string testString = "";
        string penStringTemp = "";
        string myoStringTemp = "";
        string testStringTemp = "";

        string storingString = "";

        public MainWindow()
        {
            InitializeComponent();
            receivingUdpClientMyo = new UdpClient(myoPort);
            receivingUdpClientPen = new UdpClient(penPort); ;
            receivingUdpClientTest = new UdpClient(testPort); ;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            
            startStoring();

            if(myoCheckBox.IsChecked==true)
            {
                startMyo();
            }
            if (penCheckBox.IsChecked == true)
            {
                startPen();
            }
            if (testCheckBox.IsChecked==true)
            {
                startTest();
            }
           
            
        }

        private void startStoring()
        {
            storingString = "";
            isStoring = true;
            storingThread = new Thread(new ThreadStart(storingRunningThread));
            storingThread.Start();
        }

        private void storingRunningThread()
        {
            while(isStoring)
            {
                if(penString!=penStringTemp|| myoString!=myoStringTemp || testString != testStringTemp)
                {
                    double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    storingString = "<" + now + ">" +
                        "<pen>" + penString + "</pen>" + "<myo>" + myoString + "</myo>" 
                        + "<test>" + testString + "</test></"+now+">\n";
                    penStringTemp = penString;
                    myoStringTemp = myoString;
                    testStringTemp = testString;
                }
                
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isStoring = false;
                myoRunning = false;
                penRunning = false;
                testRunning = false;
                closeTest();
                closePen();
                closeMyo();
                saveString();


            }
            catch (Exception xx)
            {
                Console.WriteLine(xx);
            }
        }

        private void saveString()
        {
            double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
            System.IO.File.WriteAllText(now+".txt", storingString);
            
        }


        #region startApps
        private void startTest()
        {
            try
            {
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process.Start(path + "/runables/"+testStringFile+".exe");
                testRunning = true;
                testThread = new Thread(new ThreadStart(testRunningThread));
                testThread.Start();
            }
            catch (Exception xx)
            {
                Console.WriteLine(xx);
            }
        }

        private void startPen()
        {
            try
            {
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process.Start(path + "/runables/"+penStringFile+".exe");
                penRunning = true;
                penThread = new Thread(new ThreadStart(penRunningThread));
                penThread.Start();
            }
            catch (Exception xx)
            {
                Console.WriteLine(xx);
            }
        }

        private void startMyo()
        {
            try
            {
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process.Start(path + "/runables/"+myoStringFile+".exe");
                myoRunning = true;
                myoThread = new Thread(new ThreadStart(myoRunningThread));
                myoThread.Start();
            }
            catch (Exception xx)
            {
                Console.WriteLine(xx);
            }
        }
        #endregion


        #region closingApps
        private void closeMyo()
        {
            try
            {
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(myoStringFile);
                pp1[0].CloseMainWindow();
            }
            catch
            {

            }
            
        }

        private void closePen()
        {
            try
            {
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(penStringFile);
                pp1[0].CloseMainWindow();
            }
            catch
            {

            }
            
        }

        private void closeTest()
        {
            try
            {
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(testStringFile);
                pp1[0].CloseMainWindow();
            }
            catch
            {

            }
            
        }
        #endregion

        #region runningthreads from apps

        private void testRunningThread()
        {
            while (testRunning == true)
            {
                //Creates an IPEndPoint to record the IP Address and port number of the sender. 
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = receivingUdpClientTest.Receive(ref RemoteIpEndPoint);

                    string returnData = Encoding.ASCII.GetString(receiveBytes);

                    Console.WriteLine("This is the message you received " +
                                                 returnData);
                    testString = returnData;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private void penRunningThread()
        {
            while (penRunning == true)
            {
                //Creates an IPEndPoint to record the IP Address and port number of the sender. 
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = receivingUdpClientPen.Receive(ref RemoteIpEndPoint);

                    string returnData = Encoding.ASCII.GetString(receiveBytes);

                    Console.WriteLine("This is the message you received " +
                                                 returnData.ToString());

                    penString = returnData.ToString();
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
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
                    myoString = returnData.ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        #endregion
    }
}
