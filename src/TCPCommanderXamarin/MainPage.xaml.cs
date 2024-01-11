using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using TCPCommanderXamarin.Droid;
using System.Text.RegularExpressions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Android.Widget;
using System.IO;
using Android.Runtime;
using System.Xml.Serialization;
using System.Net;
//using DroidOS = Android.OS;
//using Android.OS;
//using Android.App;

namespace TCPCommanderXamarin
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer

    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        string preferencesFile = "/prefs.xml";
        bool prefsExist;

        StoredData readStoredData;
        public static bool clientIsOpen;
        public static bool lostConnection;
        public bool readFile;
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (!readFile)
            {
                if (Directory.GetFiles(folderPath).Length > 0)
                {
                    if (File.Exists(folderPath + preferencesFile))
                    {
                        prefsExist = true;

                        XmlSerializer Serializer = new XmlSerializer(typeof(StoredData));
                        FileStream fs = new FileStream(folderPath + preferencesFile, FileMode.Open);

                        readStoredData = (StoredData)Serializer.Deserialize(fs);
                        ipAddress.Text = readStoredData.PrivateIPV4;
                        Port.Text = readStoredData.Port;
                        fs.Close();
                    }
                }
            }

            if (lostConnection)
            {
                ConnectionLost();
            }

            NavigationPage.SetHasBackButton(this, false);
        }

        public async void ConnectionLost()
        {
            bool response = await DisplayAlert("Client Disconnected", "The client has lost connection to the server.\nWould you like to reconnect?", "YES", "NO");

            if (response)
            {
                client.GetStream().Close();
                client.Close();
                Connect_Clicked(new object(), new EventArgs());
            }
        }

        public void lockButton(bool locked)
        {

            Device.BeginInvokeOnMainThread(() => { connectButton.IsEnabled = !locked; });
        }

        int tryConnectTime = 10000;
        public async Task<TcpClient> tryConnect()
        {
            if (client == null)
            {
                client = new TcpClient();
            }

            var connectionTask = client.ConnectAsync(ipAddress.Text, Convert.ToInt32(Port.Text)).ContinueWith(task =>
            {
                return task.IsFaulted ? null : client;
            }, TaskContinuationOptions.ExecuteSynchronously);
            var timeoutTask = Task.Delay(tryConnectTime).ContinueWith<TcpClient>(task => null, TaskContinuationOptions.ExecuteSynchronously);
            var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();
            resultTask.Wait();
            var resultTcpClient = await resultTask;

            return resultTcpClient;
        }

        public void MasterConnection()
        {
            lockButton(true);
            client = tryConnect().Result;
            lockButton(false);
        }

        TcpClient client = new TcpClient();
        private async void Connect_Clicked(object sender, EventArgs e)
        {
            try
            {
                MasterConnection();

                if (client != null)
                {
                    clientIsOpen = true;
                    Connection.Instance.client = client;
                    NavigationPage.SetHasBackButton(this, false);
                    await Navigation.PushAsync(new FunctionsPage());

                    await DisplayAlert("Connected", "Connected to server successfully!", "Ok");
                    FunctionsPage.storedprivIP = ipAddress.Text;

                    if (!prefsExist || ipAddress.Text != readStoredData.PrivateIPV4 || Port.Text != readStoredData.Port)
                    {
                        XmlSerializer Serializer = new XmlSerializer(typeof(StoredData));
                        StoredData SD = new StoredData();
                        SD.PrivateIPV4 = ipAddress.Text;
                        SD.Port = Port.Text;

                        TextWriter Writer = new StreamWriter(folderPath + preferencesFile);
                        Serializer.Serialize(Writer, SD);
                        Writer.Close();
                    }
                }
                else
                {
                    await DisplayAlert("Connection Unsuccessful", "Could not connect to the server!", "Ok");
                    clientIsOpen = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "" + ex.ToString(), "Ok");
                clientIsOpen = false;
            }
        }

        bool ipAddressIgnoreChange;
        private void IPAddress_OnTextChanged(object sender, EventArgs e)
        {
            if (ipAddressIgnoreChange)
            {
                ipAddressIgnoreChange = false;
                return;
            }


            if (Regex.IsMatch(ipAddress.Text, @"[^0-9.]"))
            {
                ipAddress.Text = Regex.Replace(ipAddress.Text, @"[^0-9.]", string.Empty);
                ipAddressIgnoreChange = true;
            }

            if (ipAddress.Text.Contains(".."))
            {
                ipAddress.Text = ipAddress.Text.Replace("..", ".");
                ipAddressIgnoreChange = true;
            }
        }

        bool portIgnoreChange;
        private void Port_OnTextChanged(object sender, EventArgs e)
        {
            if (portIgnoreChange)
            {
                portIgnoreChange = false;
                return;
            }


            if (Regex.IsMatch(Port.Text, @"[^0-9]"))
            {
                Port.Text = Regex.Replace(Port.Text, @"[^0-9]", string.Empty);
                portIgnoreChange = true;
            }
        }


    }

    public class StoredData
    {
        public string PrivateIPV4;
        public string Port;
    }

    public static class StringFunctions
    {
        public static string RemoveLetters(this string str)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                if ((str[i] >= 'a' && str[i] <= 'A') || (str[i] >= 'z' && str[i] <= 'Z'))
                {
                    sb.Append(str[i]);
                }
            }

            return sb.ToString();
        }

        public static string RemoveSpecialChars(this string str)
        {
            string[] chars = new string[] { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
            for (int i = 0; i < chars.Length; i++)
            {
                if (str.Contains(chars[i]))
                {
                    str = str.Replace(chars[i], "");
                }
            }
            return str;
        }
    }
}
