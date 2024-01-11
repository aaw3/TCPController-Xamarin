using Android;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPCommanderXamarin.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Xaml;

namespace TCPCommanderXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FunctionsPage : ContentPage
    {
        public static string storedprivIP;
        public bool pressedBackButton;

        public FunctionsPage()
        {
            InitializeComponent();
        }

        byte[] buffer = new byte[1];
        protected override void OnAppearing()
        {
            base.OnAppearing();
            NavigationPage.SetHasBackButton(this, false);

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (Connection.Instance.client != null)
                {
                    if (MainPage.clientIsOpen)
                    {
                        if (Connection.Instance.client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            if (Connection.Instance.client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                            {
                                ClientDisconnected();
                            }
                        }
                    }
                }

                return true;
            });

            Device.StartTimer(TimeSpan.FromMilliseconds(250), () =>
            {
                if (pressedBackButton)
                {
                    BackButtonPressed();
                }
                return true;
            });
        }

        protected override bool OnBackButtonPressed()
        {
            pressedBackButton = true;
            return true;
        }

        public async void BackButtonPressed()
        {
            pressedBackButton = false;
            bool response = await DisplayAlert("Are You Sure?", "Do you wan't to disconnect and go to the login page?", "YES", "NO");
            if (response)
            {
                Connection.Instance.client.GetStream().Close();
                Connection.Instance.client.Close();
                MainPage.clientIsOpen = false;
                await Navigation.PushAsync(new MainPage());
            }
        }

        public async void ClientDisconnected()
        {
            Connection.Instance.client.GetStream().Close();
            Connection.Instance.client.Close();
            MainPage.clientIsOpen = false;
            MainPage.lostConnection = true;
            await Navigation.PushAsync(new MainPage());
        }

        private void Test_Clicked(object sender, EventArgs e)
        {
            writeMessage("{TEST}");

            byte[] bytes = getData(Connection.Instance.client);

            string returnMessage = Encoding.ASCII.GetString(bytes);

            if (returnMessage == "{TEST_RESPOND}")
            {
                DisplayAlert("Test Successful", "The server received the message and responded", "OK");
            }
        }

        private async void Shutdown_Clicked(object sender, EventArgs e)
        {
            bool response = await DisplayAlert("Shutdown System", "Are you sure you want to force shutdown your computer?", "YES", "NO");
            
            if (response)
            {
                writeMessage("{SHUTDOWN}");

                byte[] bytes = getData(Connection.Instance.client);

                string returnMessage = Encoding.ASCII.GetString(bytes);

                if (returnMessage == "{BEGAN_SHUTDOWN}")
                {
                    await DisplayAlert("Shutdown Successful", "The server has received the message and has begun shutdown", "OK");
                }
            }
        }

        private void MonitorOn_Clicked(object sender, EventArgs e)
        {
            writeMessage("{MONITOR_ON}");

            byte[] bytes = getData(Connection.Instance.client);

            string returnMessage = Encoding.ASCII.GetString(bytes);

            if (returnMessage == "{MONITOR_TURNED_ON}")
            {
                DisplayAlert("Display Settings", "The server has received the message and has turned the monitor on", "OK");
            }
        }

        private void MonitorOff_Clicked(object sender, EventArgs e)
        {
            writeMessage("{MONITOR_OFF}");

            byte[] bytes = getData(Connection.Instance.client);

            string returnMessage = Encoding.ASCII.GetString(bytes);

            if (returnMessage == "{MONITOR_TURNED_OFF}")
            {
                DisplayAlert("Display Settings", "The server has received the message and has turned the monitor off", "OK");
            }
        }

        private void TakeScreenshot_Clicked(object sender, EventArgs e)
        {
            writeMessage("{TAKE_SCREENSHOT}");

            var data = getData(Connection.Instance.client);
            imageView.Source = ImageSource.FromStream(() => new MemoryStream(data));
            
        }

        Ping p = new Ping();
        PingReply reply;
        private void Ping_Clicked(object sender, EventArgs e)
        {
            int i = 0;

            int pingAmt = 8;
            int testPing = 1;

            string netInfo = "";
            do
            {
                reply = p.Send(storedprivIP, 1000);

                if (reply != null)
                {
                    i++;

                    if (i > testPing)
                    {
                        if (i <= pingAmt + 1)
                        {
                            netInfo += "Status : " + reply.Status.ToString() + "   Time : " + reply.RoundtripTime + "\n";
                        }
                        else
                        {
                            netInfo += "Status : " + reply.Status + "   Time : " + reply.RoundtripTime;
                        }
                    }
                }
                

            }
            while (i <= pingAmt + testPing
            );

            DisplayAlert("Ping: " + storedprivIP, netInfo, "OK");
        }

        public void writeMessage(string input)
        {
            TcpClient client = Connection.Instance.client;
            NetworkStream ns = client.GetStream();
            byte[] message = Encoding.ASCII.GetBytes(input);
            ns.Write(message, 0, message.Length);
        }

        public byte[] getData(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] fileSizeBytes = new byte[4];
            int bytes = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
            int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

            int bytesLeft = dataLength;
            byte[] data = new byte[dataLength];

            int buffersize = 1024;
            int bytesRead = 0;

            while (bytesLeft > 0)
            {
                int curDataSize = Math.Min(buffersize, bytesLeft);
                if (client.Available < curDataSize)
                {
                    curDataSize = client.Available;
                }

                bytes = stream.Read(data, bytesRead, curDataSize);
                bytesRead += curDataSize;
                bytesLeft -= curDataSize;
            }

            return data;
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
    }
}