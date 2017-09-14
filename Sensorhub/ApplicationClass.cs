using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sensorhub
{
    class ApplicationClass
    {
        private UdpClient receivingUdp;
        private Thread myRunningThread;
        public bool isRunning = false;
        public bool isEnabled = false;
        bool newPackage = false;
        int listeningPort;
        string filePath;
        public string applicationName;
        string currentString;

        public ApplicationClass(int listeningPort, string filePath, string applicationName)
        {
            this.listeningPort = listeningPort;
            this.filePath = filePath;
            this.applicationName = applicationName;

            receivingUdp= new UdpClient(this.listeningPort);
        }

        public bool hasNewMessage()
        {
            return newPackage;
        }
        public string getCurrentString()
        {
            newPackage = false;
            return currentString;
        }

        public void startApp()
        {
            try
            {
                string path = System.IO.Directory.GetCurrentDirectory();
                System.Diagnostics.Process.Start(filePath);
                isRunning = true;
                myRunningThread = new Thread(new ThreadStart(myThreadFunction));
                myRunningThread.Start();
            }
            catch (Exception xx)
            {
                Console.WriteLine(xx);
            }
        }

        public void closeApp()
        {
            try
            {
                myRunningThread.Abort();

                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(applicationName);
                pp1[0].CloseMainWindow();
            }
            catch (Exception xx)
            {
                Console.WriteLine("I got an exception after closing Pen" + xx);
            }
            isRunning = false;
        }


        private void myThreadFunction()
        {
            while (isRunning == true)
            {
                //Creates an IPEndPoint to record the IP Address and port number of the sender. 
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = receivingUdp.Receive(ref RemoteIpEndPoint);

                    string returnData = Encoding.ASCII.GetString(receiveBytes);

                    Console.WriteLine("This is the message you received " +
                                                 returnData);

                    currentString = returnData.ToString();
                    newPackage = true;
                }

                catch (Exception e)
                {
                    Console.WriteLine("I got an exception in the Pen thread" + e.ToString());
                }
            }
        }
    }
}
