using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using AutoNetworkSelect.NativeWifi;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace AutoNetworkSelect
{
    public partial class Main : Form
    {
        XmlSerializer SettingsSerializer = new XmlSerializer(typeof(Settings.Settings));
        Settings.Settings CurrentSettings = new Settings.Settings();

        Wlan.WlanProfileInfo preferredProfile;
        WlanClient.WlanInterface wifiInterface;

        Assembly thisAssembly;

        public Main()
        {
            InitializeComponent();

            thisAssembly = Assembly.GetExecutingAssembly();

            SystemEvents.SessionSwitch += SessionChangeHandler_SessionChanged;

            if (File.Exists("settings.xml"))
            {
                try
                {
                    CurrentSettings = SettingsSerializer.Deserialize(File.Open("settings.xml", FileMode.Open)) as Settings.Settings;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Settings Read Error");
                    Close();
                    Application.Exit();
                    Environment.Exit(0);
                }
            }
            else
            {
                SettingsSerializer.Serialize(File.Open("settings.xml", FileMode.CreateNew), new Settings.Settings());
                MessageBox.Show("settings.xml not found, created blank", "Settings Error");
                Close();
                Application.Exit();
                Environment.Exit(0);
            }

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            
            bool exists = false;
            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                foreach (Wlan.WlanProfileInfo profileInfo in wlanIface.GetProfiles())
                {
                    if (profileInfo.profileName == CurrentSettings.WifiProfile)
                    {
                        exists = true;
                        preferredProfile = profileInfo;
                        wifiInterface = wlanIface;
                    }
                }
            }

            if (!exists)
            {
                MessageBox.Show("Preferred Wifi (" + CurrentSettings.WifiProfile + ") not found, please connect to it first and remember settings", "Error");
                Close();
                Application.Exit();
                Environment.Exit(0);
            }

            exists = false;
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                if (n.Name == CurrentSettings.EthernetInterface)
                {
                    exists = true;
                }
            }

            if (!exists)
            {
                MessageBox.Show("Ethernet adapter (" + CurrentSettings.EthernetInterface + ") not found, please check adapter name", "Error");
                Close();
                Application.Exit();
                Environment.Exit(0);
            }

            exists = false;
            foreach (NetworkInterface n in adapters)
            {
                if (n.Name == CurrentSettings.WifiInterface)
                {
                    exists = true;
                }
            }

            if (!exists)
            {
                MessageBox.Show("Wifi adapter (" + CurrentSettings.WifiInterface + ") not found, please check adapter name", "Error");
                Close();
                Application.Exit();
                Environment.Exit(0);
            }

            NetworkChange_NetworkAddressChanged(null, EventArgs.Empty);
        }

        private void SessionChangeHandler_SessionChanged(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLogon || e.Reason == SessionSwitchReason.SessionUnlock)
            {
                NetworkChange_NetworkAddressChanged(null, EventArgs.Empty);
            }
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            bool isUp = false;
            bool isEthernet = false;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                if (n.Name == CurrentSettings.EthernetInterface)
                {
                    if (n.OperationalStatus == OperationalStatus.Down)
                    {
                        if (wifiInterface.InterfaceState != Wlan.WlanInterfaceState.Connected)
                            WifiConnect();
                    }
                    else if (n.OperationalStatus == OperationalStatus.Up)
                    {
                        if (wifiInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                            WifiDisconnect();

                        isEthernet = true;
                        isUp = true;
                    }
                }
                else if (n.Name == CurrentSettings.WifiInterface)
                {
                    if (n.OperationalStatus == OperationalStatus.Up)
                    {
                        isUp = true;
                    }
                }
            }

            if (isUp)
            {
                if (isEthernet)
                {
                    notifyIcon1.Icon = GetIcon("network-cable-64.ico");
                    notifyIcon1.Text = Text + ", " + CurrentSettings.EthernetInterface + ": Up";
                }
                else
                {
                    notifyIcon1.Icon = GetIcon("wifi-64.ico");
                    notifyIcon1.Text = Text + ", " + CurrentSettings.WifiInterface + ": Up";
                }
            }
            else
            {
                notifyIcon1.Icon = GetIcon("error-4-64.ico");
                notifyIcon1.Text = Text + ", " + "No connection";
            }
        }

        private Icon GetIcon(string name)
        {
            using (Stream stream = thisAssembly.GetManifestResourceStream("AutoNetworkSelect." + name))
            {
                return Icon.FromHandle(((Bitmap)Bitmap.FromStream(stream)).GetHicon());
            }
        }

        private void WifiConnect()
        {
            wifiInterface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, preferredProfile.profileName);
        }

        private void WifiDisconnect()
        {
            wifiInterface.Disconnect();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            notifyIcon1.Visible = false;
            Close();
            Application.Exit();
            Environment.Exit(0);
        }
    }
}
