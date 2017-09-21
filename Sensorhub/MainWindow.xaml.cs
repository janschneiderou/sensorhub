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
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Google.Cloud.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Cloud.Storage.V1;
using System.IO;

namespace Sensorhub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //List of applications
        List<ApplicationClass> myApps;
        List<ApplicationClass> myEnabledApps;
        TcpListener myListener;
        int tcpPort = 12001;

        //Hololens Communication
        Socket sendingToHololensSocket;
        IPEndPoint sending_end_point_to_Hololens;
        int sendingToHololensPort;
        IPAddress HololensIP;
        public bool sendToHololensFlag = true;

        //sockets that receive messages from the different applications
        //UdpClient receivingUdpClientMyo;
        //UdpClient receivingUdpClientPen;
        //UdpClient receivingUdpClientTest;


        Thread storingThread;
        bool isStoring = false;

        //Threads that receive messages from the different applications
        //Thread testThread;
        //Thread penThread;
        //Thread myoThread;
        //bool myoRunning = false;
        //bool penRunning = false;
        //bool testRunning = false;

        Thread tcpListenerThread;

        //bool penNew = false;
        //bool myoNew=false;
        //bool testNew = false;
        bool firstTime = true;

        //ports from the different applications
        //int penPort = 11001;
        //int myoPort = 11002;
        //int testPort = 11003;

        //strings for the files of the different applications
        //string penStringFilePath = "D:\\Programming repository\\PenCalligraphyWPF\\PenCalligraphyWpf\\PenCalligraphyWpf\\bin\\Debug\\PenCalligraphyWpf.exe";
        //string testStringFile = "socketTest";
        //string myoStringFile = "MyoTest";
        //string penStringFile = "PenCalligraphyWpf";

        //strings for the different applications
        //string penString = "";
        //string myoString = "";
        //string testString = "";
        //string penStringTemp = "";
        //string myoStringTemp = "";
        //string testStringTemp = "";

        string fileName = "";

        string storingString = "";

        string startString = "";
        string endString = "";

        string actor = "";
        string verb = "";
        string object1 = "";

        public MainWindow()
        {
            InitializeComponent();
            myApps = new List<ApplicationClass>();
            sendingToHololensSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            readAppsFile();
            
            //receivingUdpClientMyo = new UdpClient(myoPort);
            //receivingUdpClientPen = new UdpClient(penPort); ;
         //   receivingUdpClientTest = new UdpClient(testPort); ;
 

        }

        

        private void readAppsFile()
        {
            string path = System.IO.Directory.GetCurrentDirectory();
            string text = System.IO.File.ReadAllText(path + "\\MyApps.xml");
            getHololensInfo(text);
            int currentIndex = 0;
            try
            {
                while (text.IndexOf("Application") != -1)
                {
                    currentIndex = text.IndexOf("<Name>");
                    int startText = currentIndex + 6;
                    string applicationName = text.Substring(startText, text.IndexOf("</Name>") - startText);
                    text = text.Substring(text.IndexOf("</Name>"));
                    currentIndex = text.IndexOf("<Path>");
                    startText = currentIndex + 6;
                    string filePath = text.Substring(startText, text.IndexOf("</Path>") - startText);
                    text = text.Substring(text.IndexOf("</Path>"));
                    currentIndex = text.IndexOf("<Port>");
                    startText = currentIndex + 6;
                    string listeningPortString = text.Substring(startText, text.IndexOf("</Port>") - startText);
                    int listeningPort = int.Parse(listeningPortString);
                    text = text.Substring(text.IndexOf("</Application>") + 3);

                    ApplicationClass app = new ApplicationClass(listeningPort, filePath, applicationName);
                    myApps.Add(app);
                    currentIndex++;
                }
            }
            catch
            {
                Console.WriteLine("I got an exception when reading configuration for Applications");
            }
            
        }

        


        private void Window_Initialized(object sender, EventArgs e)
        {
            tcpListenerThread = new Thread(new ThreadStart(tcpListenersStart));
            tcpListenerThread.Start();
        }

   

        private void storingRunningThread()
        {
            while(isStoring)
            {
                bool newMessages = false;
                string storingStringTemp = "";
                foreach(ApplicationClass app in myEnabledApps)
                {
                    if(app.hasNewMessage()==true)
                    {
                        newMessages = true;
                    }
                }
                if(newMessages==true)
                {
                    double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    bool thereIsSomething = false;
                    if (firstTime == true)
                    {

                        storingStringTemp = storingStringTemp + "{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                        firstTime = false;
                    }
                    else
                    {
                        storingStringTemp = storingStringTemp + ",{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                    }
                    foreach(ApplicationClass app in myApps)
                    {
                        if (app.hasNewMessage() == true)
                        {
                            if (thereIsSomething == true)
                            {
                                storingStringTemp = storingStringTemp + ",";
                            }

                            storingStringTemp = storingStringTemp + app.getCurrentString();
                            thereIsSomething = true;
                        }
                    }
                    storingStringTemp = storingStringTemp + " ]}" + Environment.NewLine;

                    if(sendToHololensFlag==true)
                    {
                        sendToHololens(storingStringTemp);
                    }
                    storingString = storingString + storingStringTemp;

                }

                //if (penNew == true || myoNew == true || testNew ==true)
                //{
                //    double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
                //    bool thereIsSomething = false;
                //    if (firstTime==true)
                //    {
                        
                //        storingString = storingString + "{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                //        firstTime = false;
                //    }
                //    else
                //    {
                //        storingString = storingString + ",{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                //    }
                    
                //    if (penNew==true)
                //    {
                //        if(thereIsSomething==true)
                //        {
                //            storingString = storingString + ",";
                //        }
                //        penNew = false;
                //        storingString = storingString + penString;
                //        thereIsSomething = true;
                //    }
                //    if (myoNew==true)
                //    {
                //        if (thereIsSomething == true)
                //        {
                //            storingString = storingString + ",";
                //        }
                //        storingString = storingString + myoString;
                //        myoNew = false;
                //    }
                //    if(testNew==true)
                //    {
                //        if (thereIsSomething == true)
                //        {
                //            storingString = storingString + ",";
                //        }
                //        storingString = storingString + testString;
                //        testNew = false;
                //    }
                //    storingString = storingString + " ]}"+ Environment.NewLine;
                        
           
                        
                //}
                Thread.Sleep(17);
                
            }
        }

        

        private void saveString()
        {
            double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
            fileName = now + ".json";
            startString = "{\"idSensorFile\":\"" + fileName + "\",  \"actor\":\"" + actor + "\",\"verb\":\"" + verb +
                "\",\"object\":\"" + object1 + "\",\"SensorUpdates\":[" + Environment.NewLine;
            endString = Environment.NewLine + "]}";
            storingString = startString + storingString + endString;


            System.IO.File.WriteAllText(fileName, storingString);

        }


        #region startAndStopRecording
        private void Start_Click(object sender, RoutedEventArgs e)
        {

            //if (myoCheckBox.IsChecked == true)
            //{
            //    startMyo();
            //}
            //if (penCheckBox.IsChecked == true)
            //{
            //    startPen();
            //}
            //if (testCheckBox.IsChecked==true)
            //{
            //    startTest();
            //}

            //new stuff with configFile

            setEnabledApps();

            foreach (ApplicationClass app in myEnabledApps)
            {
                app.startApp();
            }
            startStoring();

        }

        //TODO Change the name of the strings according to the name of the actual applications. 
        // Right now it uses checkboxes to see if sensor would be used for the recording, this might change
        private void setEnabledApps()
        {
            myEnabledApps = new List<ApplicationClass>();

            if (myoCheckBox.IsChecked == true)
            {
                foreach (ApplicationClass app in myApps)
                {
                    if (app.applicationName.Equals("MyoTest"))
                    {
                        app.isEnabled = true;
                        myEnabledApps.Add(app);
                        break;
                    }
                }
            }
            if (penCheckBox.IsChecked == true)
            {
                foreach (ApplicationClass app in myApps)
                {
                    if (app.applicationName.Equals("PenCalligraphyWpf"))
                    {
                        app.isEnabled = true;
                        myEnabledApps.Add(app);
                        break;
                    }
                }
            }
            if (testCheckBox.IsChecked == true)
            {
                foreach (ApplicationClass app in myApps)
                {
                    if (app.applicationName.Equals("socketTest")|| app.applicationName.Equals("LeapMotionTest"))
                    {
                        app.isEnabled = true;
                        myEnabledApps.Add(app);
                        break;
                    }
                }
            }
        }

        private void startStoring()
        {
            storingString = "";
            isStoring = true;
            storingThread = new Thread(new ThreadStart(storingRunningThread));
            storingThread.Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //  storingThread.Abort();


                isStoring = false;
                //myoRunning = false;
                //penRunning = false;
                //testRunning = false;
                foreach (ApplicationClass app in myEnabledApps)
                {
                    app.closeApp();
                }

                //closeTest();
                //closePen();
                //closeMyo();
                saveString();
                //  googleCloudSave();
                  googleCloudBucket();

            }
            catch (Exception xx)
            {
                Console.WriteLine("I got an exception after the stop click" + xx);
            }
        }
        #endregion

        #region TCPListeningStartingStopping
        private void tcpListenersStart()
        {
            myListener = new TcpListener(IPAddress.Any, tcpPort);
            myListener.Start();
            while (true)
            {
                Console.WriteLine("The server is running at port 12001...");
                Console.WriteLine("The local End point is  :" +
                                  myListener.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Socket s = myListener.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

                byte[] b = new byte[100];

                int k = s.Receive(b);
                Console.WriteLine("Recieved...");
                string receivedString = System.Text.Encoding.UTF8.GetString(b);


                for (int i = 0; i < k; i++)
                {
                    Console.Write(Convert.ToChar(b[i]));
                }
                if (receivedString.Contains("start"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Start_Click(null, null);
                    });


                }
                if (receivedString.Contains("stop"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Stop_Click(null, null);
                    });

                }

                ASCIIEncoding asen = new ASCIIEncoding();
                s.Send(asen.GetBytes("The string was recieved by the server."));
                Console.WriteLine("\nSent Acknowledgement");
            }

        }

        #endregion

        #region SavingToCloud
        private void googleCloudBucket()
        {
            // Your Google Cloud Platform project ID.
            string projectId = "dojo-ibl";


            // Instantiates a client.
            StorageClient storageClient = StorageClient.Create();

            // The name for the new bucket.
            string bucketName =  "test-pipeline-plumbers";
            try
            {
                // Creates the new bucket.
                //  storageClient.CreateBucket(projectId, bucketName);
                
                string path = Directory.GetCurrentDirectory();
                path = path + "\\" + fileName;
                
                using (var fileStream = File.OpenRead(path))
                {
                    path = path + "\\" + fileName;
                    string objectName = System.IO.Path.GetFileName(path);

                    storageClient.UploadObject(bucketName, objectName, null, fileStream);
                    Console.WriteLine($" file {fileName} send to Bucket {bucketName} ");
                }

               
            }
            catch (Google.GoogleApiException e)
            when (e.Error.Code == 409)
            {
                // The bucket already exists.  That's fine.
                Console.WriteLine(e.Error.Message);
            }
        }

        private async void googleCloudSave()
        {

            //GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

            //var compute = new ComputeService(new BaseClientService.Initializer()
            //{
            //    HttpClientInitializer = credential
            //});

            //if (credential.IsCreateScopedRequired)
            //{
            //    credential = credential.CreateScoped(new[] { ComputeService.Scope.CloudPlatform, ComputeService.Scope.Compute });
            //}
            // Instantiates a client
            PublisherClient publisher = PublisherClient.Create();

            TopicName topicName = new TopicName("dojo-ibl", "new_tweets");
            PubsubMessage message = new PubsubMessage
            {
                // The data is any arbitrary ByteString. Here, we're using text.
                Data = ByteString.CopyFromUtf8(storingString),
                // The attributes provide metadata in a string-to-string 
                // dictionary.
                Attributes =
                    {
                        { "fubar", "fuckedupbeyondallrecognition" }
                        }
            };
            
           try
           {
              publisher.Publish(topicName, new[] { message });
           }
          catch(Exception x)
          {
                Console.WriteLine("Hello!!!." + x);
          }
            Console.WriteLine("Topic message created.");


        }

        #endregion

        #region Hololens_Configuration_and_Communication

        private void getHololensInfo(string text)
        {
            try
            {
                int startIndex = text.IndexOf("<HololensIP>") + 12;
                int endIndex = text.IndexOf("</HololensIP>");
                string holoIP = text.Substring(startIndex, endIndex - startIndex);
                HololensIP = IPAddress.Parse(holoIP);

                startIndex = text.IndexOf("<HololensPort>") + 14;
                endIndex = text.IndexOf("</HololensPort>");
                string port = text.Substring(startIndex, endIndex - startIndex);

                sendingToHololensPort = int.Parse(port);
                sending_end_point_to_Hololens = new IPEndPoint(HololensIP, sendingToHololensPort);
            }
            catch
            {
                Console.WriteLine("I got an exception when reading configuration for Hololens");
            }

        }

        private void sendToHololens(string storingStringTemp)
        {

            byte[] send_buffer = Encoding.ASCII.GetBytes(storingStringTemp);
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = sending_end_point_to_Hololens;

            try
            {
                socketEventArg.SetBuffer(send_buffer, 0, send_buffer.Length);
                sendingToHololensSocket.SendToAsync(socketEventArg);
              
            }
            catch
            {
                Console.WriteLine("error sending message");
            }
            //try
            //{
            //    sendingToHololensSocket.SendTo(send_buffer, sending_end_point_to_Hololens);
            //}
            //catch (Exception send_exception)
            //{

            //    Console.WriteLine(" Exception {0}", send_exception.Message);
            //}
        }

        private void SendToHololensCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SendToHololensCheckBox.IsChecked == true)
            {
                sendToHololensFlag = true;
            }
            else
            {
                sendToHololensFlag = false;
            }
        }

        #endregion


        //This might go away!!
        #region startApps
        //private void startTest()
        //{
        //    try
        //    {
        //        string path = System.IO.Directory.GetCurrentDirectory();
        //        System.Diagnostics.Process.Start(path + "/runables/"+testStringFile+".exe");
        //        testRunning = true;
        //        testThread = new Thread(new ThreadStart(testRunningThread));
        //        testThread.Start();
        //    }
        //    catch (Exception xx)
        //    {
        //        Console.WriteLine(xx);
        //    }
        //}

        //private void startPen()
        //{
        //    try
        //    {
        //        string path = System.IO.Directory.GetCurrentDirectory();
        //        System.Diagnostics.Process.Start(penStringFilePath);
        //        penRunning = true;
        //        penThread = new Thread(new ThreadStart(penRunningThread));
        //        penThread.Start();
        //    }
        //    catch (Exception xx)
        //    {
        //        Console.WriteLine(xx);
        //    }
        //}

        //private void startMyo()
        //{
        //    try
        //    {
        //        string path = System.IO.Directory.GetCurrentDirectory();
        //        System.Diagnostics.Process.Start(path + "/runables/"+myoStringFile+".exe");
        //        myoRunning = true;
        //        myoThread = new Thread(new ThreadStart(myoRunningThread));
        //        myoThread.Start();
        //    }
        //    catch (Exception xx)
        //    {
        //        Console.WriteLine(xx);
        //    }
        //}
        #endregion


        #region closingApps
        //private void closeMyo()
        //{
        //    try
        //    {
        //        myoThread.Abort();
        //        System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(myoStringFile);
        //        pp1[0].CloseMainWindow();
        //    }
        //    catch (Exception xx)
        //    {
        //        Console.WriteLine("I got an exception after closing Myo" + xx);
        //    }

        //}

        //private void closePen()
        //{
        //    try
        //    {
        //        penThread.Abort();

        //        System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(penStringFile);
        //        pp1[0].CloseMainWindow();
        //    }
        //    catch (Exception xx)
        //    {
        //        Console.WriteLine("I got an exception after closing Pen" + xx);
        //    }

        //}

        //private void closeTest()
        //{
        //    try
        //    {
        //        System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(testStringFile);
        //        pp1[0].CloseMainWindow();
        //    }
        //    catch(Exception xx)
        //    {
        //        Console.WriteLine("I got an exception after closing Myo" + xx);
        //    }

        //}
        #endregion

        #region runningthreads from apps

        //private void testRunningThread()
        //{
        //    while (testRunning == true)
        //    {
        //        //Creates an IPEndPoint to record the IP Address and port number of the sender. 
        //        // The IPEndPoint will allow you to read datagrams sent from any source.
        //        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //        try
        //        {

        //            // Blocks until a message returns on this socket from a remote host.
        //            Byte[] receiveBytes = receivingUdpClientTest.Receive(ref RemoteIpEndPoint);

        //            string returnData = Encoding.ASCII.GetString(receiveBytes);

        //            Console.WriteLine("This is the message you received " +
        //                                         returnData);
        //            testString = returnData;
        //            testNew = true;
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("I got an exception in the test thread" + e.ToString());
        //        }
        //    }
        //}

        //private void penRunningThread()
        //{
        //    while (penRunning == true)
        //    {
        //        //Creates an IPEndPoint to record the IP Address and port number of the sender. 
        //        // The IPEndPoint will allow you to read datagrams sent from any source.
        //        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //        try
        //        {

        //            // Blocks until a message returns on this socket from a remote host.
        //            Byte[] receiveBytes = receivingUdpClientPen.Receive(ref RemoteIpEndPoint);

        //            string returnData = Encoding.ASCII.GetString(receiveBytes);

        //            Console.WriteLine("This is the message you received " +
        //                                         returnData);

        //            penString = returnData.ToString();
        //            penNew = true;
        //        }

        //        catch (Exception e)
        //        {
        //            Console.WriteLine("I got an exception in the Pen thread" + e.ToString());
        //        }
        //    }
        //}

        //private void myoRunningThread()
        //{
        //    while (myoRunning == true)
        //    {
        //        //Creates an IPEndPoint to record the IP Address and port number of the sender. 
        //        // The IPEndPoint will allow you to read datagrams sent from any source.
        //        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //        try
        //        {

        //            // Blocks until a message returns on this socket from a remote host.
        //            Byte[] receiveBytes = receivingUdpClientMyo.Receive(ref RemoteIpEndPoint);

        //            string returnData = Encoding.ASCII.GetString(receiveBytes);

        //            Console.WriteLine("This is the message you received " +
        //                                         returnData.ToString());
        //            myoString = returnData.ToString();
        //            myoNew = true;
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("I got an exception in the Myo thread" + e.ToString());
        //        }
        //    }
        //}
        #endregion




    }
}
