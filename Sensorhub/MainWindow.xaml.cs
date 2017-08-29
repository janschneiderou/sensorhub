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

        bool penNew = false;
        bool myoNew=false;
        bool testNew = false;
        bool firstTime = true;

        //ports from the different applications
        int penPort = 11001;
        int myoPort = 11002;
        int testPort = 11003;

        //strings for the files of the different applications
        string penStringFilePath = "D:\\Programming repository\\PenCalligraphyWPF\\PenCalligraphyWpf\\PenCalligraphyWpf\\bin\\Debug\\PenCalligraphyWpf.exe";
        string testStringFile = "socketTest";
        string myoStringFile = "MyoTest";
        string penStringFile = "PenCalligraphyWpf";

        //strings for the different applications
        string penString = "";
        string myoString = "";
        string testString = "";
        string penStringTemp = "";
        string myoStringTemp = "";
        string testStringTemp = "";

        string fileName = "";

        string storingString = "";

        string startString = "";
        string endString = "";

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
                if (penNew == true || myoNew == true || testNew ==true)
                {
                    double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    bool thereIsSomething = false;
                    if (firstTime==true)
                    {
                        
                        storingString = storingString + "{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                        firstTime = false;
                    }
                    else
                    {
                        storingString = storingString + ",{ \"Timestamp\":\"" + now + "\",\"Sensors\":[";
                    }
                    
                    if (penNew==true)
                    {
                        if(thereIsSomething==true)
                        {
                            storingString = storingString + ",";
                        }
                        penNew = false;
                        storingString = storingString + penString;
                        thereIsSomething = true;
                    }
                    if (myoNew==true)
                    {
                        if (thereIsSomething == true)
                        {
                            storingString = storingString + ",";
                        }
                        storingString = storingString + myoString;
                        myoNew = false;
                    }
                    if(testNew==true)
                    {
                        if (thereIsSomething == true)
                        {
                            storingString = storingString + ",";
                        }
                        storingString = storingString + testString;
                        testNew = false;
                    }
                    storingString = storingString + " ]}"+ Environment.NewLine;
                        
           
                        
                }
                Thread.Sleep(17);
                
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //  storingThread.Abort();

                
                isStoring = false;
                myoRunning = false;
                penRunning = false;
                testRunning = false;
                closeTest();
                closePen();
                closeMyo();
                saveString();
              //  googleCloudSave();
                googleCloudBucket();

            }
            catch (Exception xx)
            {
                Console.WriteLine("I got an exception after the stop click" + xx);
            }
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
        

        private void saveString()
        {
            double now = DateTime.Now.TimeOfDay.TotalMilliseconds;
            fileName = now + ".json";
            startString = "{\"idSensorFile\":\"" + fileName + "\",\"SensorUpdates\":["+Environment.NewLine;
            endString = Environment.NewLine+"]}";
            storingString = startString + storingString + endString;


            System.IO.File.WriteAllText(fileName, storingString);
            


            
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
                System.Diagnostics.Process.Start(penStringFilePath);
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
                myoThread.Abort();
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(myoStringFile);
                pp1[0].CloseMainWindow();
            }
            catch (Exception xx)
            {
                Console.WriteLine("I got an exception after closing Myo" + xx);
            }
            
        }

        private void closePen()
        {
            try
            {
                penThread.Abort();
               
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(penStringFile);
                pp1[0].CloseMainWindow();
            }
            catch (Exception xx)
            {
                Console.WriteLine("I got an exception after closing Pen" + xx);
            }
            
        }

        private void closeTest()
        {
            try
            {
                System.Diagnostics.Process[] pp1 = System.Diagnostics.Process.GetProcessesByName(testStringFile);
                pp1[0].CloseMainWindow();
            }
            catch(Exception xx)
            {
                Console.WriteLine("I got an exception after closing Myo" + xx);
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
                    testNew = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("I got an exception in the test thread" + e.ToString());
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
                                                 returnData);

                    penString = returnData.ToString();
                    penNew = true;
                }

                catch (Exception e)
                {
                    Console.WriteLine("I got an exception in the Pen thread" + e.ToString());
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
                    myoNew = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("I got an exception in the Myo thread" + e.ToString());
                }
            }
        }
        #endregion
    }
}
