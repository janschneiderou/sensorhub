﻿using System;
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
using System.Net.Http;

namespace Sensorhub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly HttpClient httpClient = new HttpClient();

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
        public  bool sendToHololensFlag = true;

        string  myOAuthToken;
        string fireBaseToken;

        public  bool directPush = false;
        Thread storingThread;
        bool isStoring = false;
        bool startedWithTCP = false;


        Thread tcpListenerThread;

        
        bool firstTime = true;



        string fileName = "";

        string storingString = "";

        string startString = "";
        string endString = "";

        string XAPI = "";
        string actor = "";
        string verb = "";
        string object1 = "";

        #region initialization
        public MainWindow()
        {
            InitializeComponent();
            myApps = new List<ApplicationClass>();
            myEnabledApps = new List<ApplicationClass>();
            sendingToHololensSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            readAppsFile();
           
            //receivingUdpClientMyo = new UdpClient(myoPort);
            //receivingUdpClientPen = new UdpClient(penPort); ;
         //   receivingUdpClientTest = new UdpClient(testPort); ;
 

        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            tcpListenerThread = new Thread(new ThreadStart(tcpListenersStart));
            tcpListenerThread.Start();
        }
        #endregion

        #region readConfigurationFile
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

                    ApplicationClass app = new ApplicationClass(listeningPort, filePath, applicationName, this);
                    myApps.Add(app);
                    currentIndex++;
                }
            }
            catch
            {
                Console.WriteLine("I got an exception when reading configuration for Applications");
            }
            
        }

        #endregion


        #region creatingtheJSONString
        //used for directPushed Method is called from applicationClass
        public  void storeString(string currentString)
        {
            DateTime now = DateTime.Now;

            string nowStringX = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
 
            
            string storingStringTemp = "";
            if (firstTime == true)
            {

                storingStringTemp = storingStringTemp + "{ \"Timestamp\":\"" + nowStringX + "\",\"Sensors\":[";
                firstTime = false;
            }
            else
            {
                storingStringTemp = storingStringTemp + ",{ \"Timestamp\":\"" + nowStringX + "\",\"Sensors\":[";
            }
            storingStringTemp = storingStringTemp + currentString;
            storingStringTemp = storingStringTemp + " ]}" + Environment.NewLine;

            if (sendToHololensFlag == true)
            {
                sendToHololens(storingStringTemp);
            }
            storingString = storingString + storingStringTemp;
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
                    DateTime now = DateTime.Now;

                    string nowStringX = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    bool thereIsSomething = false;
                    if (firstTime == true)
                    {

                        storingStringTemp = storingStringTemp + "{ \"Timestamp\":\"" + nowStringX + "\",\"Sensors\":[";
                        firstTime = false;
                    }
                    else
                    {
                        storingStringTemp = storingStringTemp + ",{ \"Timestamp\":\"" + nowStringX + "\",\"Sensors\":[";
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
               // Thread.Sleep(17);
                
            }
        }

        //Saves the JSON String to a file
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

        #endregion




        #region startAndStopRecording
        private void Start_Click(object sender, RoutedEventArgs e)
        {


            //new stuff with configFile
            if(startedWithTCP==false)
            {
                setEnabledApps();
            }
            startedWithTCP = false;
            firstTime = true;
            foreach (ApplicationClass app in myEnabledApps)
            {
                app.startApp();
            }
            if(directPush==false)
            {
                startStoring();
            }
            

        }

        // This method is used only for the button Click Start if the start comes from the TCP this method is ignored
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



                isStoring = false;

                foreach (ApplicationClass app in myEnabledApps)
                {
                    app.closeApp();
                }

                saveString();
  
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
                // Token:
                if(receivedString.Contains("Token"))
                {
                    parseToken(receivedString);
                    firebaseAuth();
                }
                if (receivedString.Contains("start"))
                {
                    if(isStoring==false)
                    {
                        myEnabledApps.Clear();
                        getParametersFromTCPString(receivedString);
                       
                        Dispatcher.Invoke(() =>
                        {
                            Start_Click(null, null);
                        });
                    }
                    


                }
                else if (receivedString.Contains("stop"))
                {
                    if(isStoring==true)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Stop_Click(null, null);
                        });
                    }
                    

                }

                ASCIIEncoding asen = new ASCIIEncoding();
                s.Send(asen.GetBytes("The string was recieved by the server."));
                Console.WriteLine("\nSent Acknowledgement");
            }

        }
        //Token:heregoesthetoken
        private void parseToken(string receivedString)
        {
            myOAuthToken = receivedString.Substring(6);
        }

        /* 
* the string to start a recording should be somethink like:
* start actor=Fridolin; verb=writes; object=test; directPush=true; apps=app1;app2;app3; 
* or 
* start <XAPI>actor=Fridolin; verb=writes; object=test; </XAPI> directPush=true; apps=app1;app2;app3; 
* where app1 app2 app3 are the apps running the sensors that will be used for the recording. 
* */
        private void getParametersFromTCPString(string receivedString)
        {
            int start;
            int length=0;

            //Parse XAPI
            start= receivedString.IndexOf("<XAPI>");
            if(start > -1)
            {
                start = start + 6;
                length = receivedString.IndexOf("</XAPI>", start) - start;
            }
            if (start > -1 && length > 0)
            {
                XAPI = receivedString.Substring(start, length);
                length = 0;
            }
            //Parse actor
            start = receivedString.IndexOf("actor=");
            if(start>-1)
            {
                start = start + 6;
                length = receivedString.IndexOf(";", start) - start;
            }
           
            if(start>-1 && length>0)
            {
                actor = receivedString.Substring(start, length);
                length = 0;
            }


            // Parse verb
            start = receivedString.IndexOf("verb=") ;
            if (start > -1)
            {
                start = start + 5;
                length = receivedString.IndexOf(";", start) - start;
            }
            
            if (start > -1 && length > 0)
            {
                verb = receivedString.Substring(start, length);
                length = 0;
            }

            // Parse object
            start = receivedString.IndexOf("object=");
            if (start > -1)
            {
                start = start + 7;
                length = receivedString.IndexOf(";", start) - start;
            }
            
            if (start > -1 && length > 0)
            {
                object1 = receivedString.Substring(start, length);
                length = 0;
            }


            // Parse directPush
            start = receivedString.IndexOf("directPush=");
            if (start > -1)
            {
                start = start + 11;
                length = receivedString.IndexOf(";", start) - start;
            }

            if (start > -1 && length > 0)
            {
                string dP = receivedString.Substring(start, length);
                if(dP.Equals("true"))
                {
                    directPush = true;
                }
                length = 0;
            }


            start = receivedString.IndexOf("apps=") + 5;
            string appStrings = receivedString.Substring(start);

            while (appStrings.IndexOf(";") >= 0)
            {
                length = appStrings.IndexOf(";");
                enableApp(appStrings.Substring(0,length));
                appStrings = appStrings.Substring(length+1);
            }
          
            startedWithTCP = true;

        }

        private void enableApp(string v)
        {
            foreach(ApplicationClass app in myApps)
            {
                if(v.Equals(app.applicationName))
                {
                    app.isEnabled = true;
                    myEnabledApps.Add(app);
                    break;
                }
            }
        }

        #endregion

        #region SavingToCloud

        private async void firebaseAuth()
        {
            //var values = new Dictionary<string, string>
            //{
            //    { "token", myOAuthToken }
            //};

            //var content = new FormUrlEncodedContent(values);

            //var response = await httpClient.PostAsync("https://wekitproject.appspot.com/", content);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://192.168.0.199:3000/");
            request.Headers.Add("Authorization", "Bearer " + myOAuthToken);
           // request.Headers["Bearer"] = myOAuthToken;
            
            

            var response1 = request.GetResponse();
            var response= await request.GetResponseAsync();

            // fireBaseToken = await response.Content.ReadAsStringAsync();
            int x = 1;
        }

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

        //This might go away!!
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

        //This might go away!!
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            myOAuthToken = "Fridolin";
            firebaseAuth();
        }
    }
}
