﻿// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TrakHound;
using TrakHound.API.Users;
using TrakHound.Configurations;
using TrakHound.Configurations.AutoGenerate;
using TrakHound.Tools;

namespace TrakHound_Dashboard.Pages.DeviceManager.AddDevice.Pages
{
    /// <summary>
    /// Page containing options for adding Devices that were automatically found on the network
    /// </summary>
    public partial class AutoDetect : UserControl, IPage
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private object _lock = new object();

        public AutoDetect()
        {
            InitializeComponent();
            DataContext = this;

            PingTimeout = Math.Max(100, Properties.Settings.Default.AutoDetectPingTimeout);

            StartPort = Properties.Settings.Default.AutoDetectStartPort;
            EndPort = Properties.Settings.Default.AutoDetectEndPort;

            StartAddress = Properties.Settings.Default.AutoDetectStartAddress;
            EndAddress = Properties.Settings.Default.AutoDetectEndAddress;

            if (string.IsNullOrEmpty(StartAddress) || string.IsNullOrEmpty(EndAddress))
            {
                SetDefaultAddressRange();
            }
        }

        #region "IPage"

        public string Title { get { return "Auto Detect"; } }

        public Uri Image { get { return new Uri("pack://application:,,,/TrakHound-Dashboard;component/Resources/Options_01.png"); } }

        public bool ZoomEnabled { get { return false; } }

        public void SetZoom(double zoomPercentage) { }

        public void Opened() { }
        public bool Opening() { return true; }

        public void Closed() { }
        public bool Closing()
        {
            // Save Auto Detect Setttings

            Properties.Settings.Default.AutoDetectStartPort = StartPort;
            Properties.Settings.Default.AutoDetectEndPort = EndPort;

            Properties.Settings.Default.AutoDetectPingTimeout = PingTimeout;

            Properties.Settings.Default.AutoDetectStartAddress = StartAddress;
            Properties.Settings.Default.AutoDetectEndAddress = EndAddress;

            Properties.Settings.Default.Save();

            Cancel();
            return true;
        }

        public event SendData_Handler SendData;


        private ObservableCollection<DeviceDescription> _devices;
        /// <summary>
        /// Collection of TrakHound.Configurations.Configuration objects that represent the active devices
        /// </summary>
        public ObservableCollection<DeviceDescription> Devices
        {
            get
            {
                if (_devices == null)
                    _devices = new ObservableCollection<DeviceDescription>();
                return _devices;
            }

            set
            {
                _devices = value;
            }
        }

        private UserConfiguration currentUser;


        public void GetSentData(EventData data)
        {
            Dispatcher.BeginInvoke(new Action<EventData>(UpdateLoggedInChanged), System.Windows.Threading.DispatcherPriority.DataBind, new object[] { data });
            Dispatcher.BeginInvoke(new Action<EventData>(UpdateDevicesLoading), System.Windows.Threading.DispatcherPriority.Normal, new object[] { data });

            Dispatcher.BeginInvoke(new Action<EventData>(UpdateDeviceAdded), System.Windows.Threading.DispatcherPriority.DataBind, new object[] { data });
            Dispatcher.BeginInvoke(new Action<EventData>(UpdateDeviceUpdated), System.Windows.Threading.DispatcherPriority.DataBind, new object[] { data });
            Dispatcher.BeginInvoke(new Action<EventData>(UpdateDeviceRemoved), System.Windows.Threading.DispatcherPriority.DataBind, new object[] { data });
        }

        void UpdateLoggedInChanged(EventData data)
        {
            if (data != null)
            {
                if (data.Id == "USER_LOGIN")
                {
                    if (data.Data01 != null) currentUser = (UserConfiguration)data.Data01;
                }
                else if (data.Id == "USER_LOGOUT")
                {
                    currentUser = null;
                }
            }
        }

        void UpdateDevicesLoading(EventData data)
        {
            if (data != null)
            {
                if (data.Id == "DEVICES_LOADING")
                {
                    DevicesLoading = true;
                    ClearDevices();
                }

                if (data.Id == "DEVICES_LOADED")
                {
                    DevicesLoading = false;
                }
            }
        }

        void UpdateDeviceAdded(EventData data)
        {
            if (data != null)
            {
                if (data.Id == "DEVICE_ADDED" && data.Data01 != null)
                {
                    Devices.Add((DeviceDescription)data.Data01);
                }
            }
        }

        void UpdateDeviceUpdated(EventData data)
        {
            if (data != null)
            {
                if (data.Id == "DEVICE_UPDATED" && data.Data01 != null)
                {
                    var device = (DeviceDescription)data.Data01;

                    int i = Devices.ToList().FindIndex(x => x.UniqueId == device.UniqueId);
                    if (i >= 0)
                    {
                        Devices.RemoveAt(i);
                        Devices.Insert(i, device);
                    }
                }
            }
        }

        void UpdateDeviceRemoved(EventData data)
        {
            if (data != null)
            {
                if (data.Id == "DEVICE_REMOVED" && data.Data01 != null)
                {
                    var device = (DeviceDescription)data.Data01;

                    int i = Devices.ToList().FindIndex(x => x.UniqueId == device.UniqueId);
                    if (i >= 0)
                    {
                        Devices.RemoveAt(i);
                    }
                }
            }
        }

        private void ClearDevices()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Devices.Clear();
            }
            ), System.Windows.Threading.DispatcherPriority.DataBind, new object[] { });
        }

        #endregion

        /// <summary>
        /// Parent AddDevice.Page object
        /// </summary>
        public Page ParentPage { get; set; }

        #region "Dependency Properties"

        /// <summary>
        /// Used to tell if the Devices are currently being loaded
        /// </summary>
        public bool DevicesLoading
        {
            get { return (bool)GetValue(DevicesLoadingProperty); }
            set { SetValue(DevicesLoadingProperty, value); }
        }

        public static readonly DependencyProperty DevicesLoadingProperty =
            DependencyProperty.Register("DevicesLoading", typeof(bool), typeof(AutoDetect), new PropertyMetadata(false));

        /// <summary>
        /// Number of Devices that were found that have already been added
        /// </summary>
        public int DevicesAlreadyAdded
        {
            get { return (int)GetValue(DevicesAlreadyAddedProperty); }
            set { SetValue(DevicesAlreadyAddedProperty, value); }
        }

        public static readonly DependencyProperty DevicesAlreadyAddedProperty =
            DependencyProperty.Register("DevicesAlreadyAdded", typeof(int), typeof(AutoDetect), new PropertyMetadata(0));

        /// <summary>
        /// Number of Devices that were found but haven't been added yet
        /// </summary>
        public int DevicesNotAdded
        {
            get { return (int)GetValue(DevicesNotAddedProperty); }
            set { SetValue(DevicesNotAddedProperty, value); }
        }

        public static readonly DependencyProperty DevicesNotAddedProperty =
            DependencyProperty.Register("DevicesNotAdded", typeof(int), typeof(AutoDetect), new PropertyMetadata(0));

        /// <summary>
        /// Number of Network Nodes found using Ping
        /// </summary>
        public int NetworkNodesFound
        {
            get { return (int)GetValue(NetworkNodesFoundProperty); }
            set { SetValue(NetworkNodesFoundProperty, value); }
        }

        public static readonly DependencyProperty NetworkNodesFoundProperty =
            DependencyProperty.Register("NetworkNodesFound", typeof(int), typeof(AutoDetect), new PropertyMetadata(0));


        public bool DetailsShown
        {
            get { return (bool)GetValue(DetailsShownProperty); }
            set { SetValue(DetailsShownProperty, value); }
        }

        public static readonly DependencyProperty DetailsShownProperty =
            DependencyProperty.Register("DetailsShown", typeof(bool), typeof(AutoDetect), new PropertyMetadata(true));


        public string DetailsText
        {
            get { return (string)GetValue(DetailsTextProperty); }
            set { SetValue(DetailsTextProperty, value); }
        }

        public static readonly DependencyProperty DetailsTextProperty =
            DependencyProperty.Register("DetailsText", typeof(string), typeof(AutoDetect), new PropertyMetadata(null));


        public double SearchProgressValue
        {
            get { return (double)GetValue(SearchProgressValueProperty); }
            set { SetValue(SearchProgressValueProperty, value); }
        }

        public static readonly DependencyProperty SearchProgressValueProperty =
            DependencyProperty.Register("SearchProgressValue", typeof(double), typeof(AutoDetect), new PropertyMetadata(0d));


        public double SearchProgressMaximum
        {
            get { return (double)GetValue(SearchProgressMaximumProperty); }
            set { SetValue(SearchProgressMaximumProperty, value); }
        }

        public static readonly DependencyProperty SearchProgressMaximumProperty =
            DependencyProperty.Register("SearchProgressMaximum", typeof(double), typeof(AutoDetect), new PropertyMetadata(1d));


        public string StartAddress
        {
            get { return (string)GetValue(StartAddressProperty); }
            set { SetValue(StartAddressProperty, value); }
        }

        public static readonly DependencyProperty StartAddressProperty =
            DependencyProperty.Register("StartAddress", typeof(string), typeof(AutoDetect), new PropertyMetadata(null));

        public string EndAddress
        {
            get { return (string)GetValue(EndAddressProperty); }
            set { SetValue(EndAddressProperty, value); }
        }

        public static readonly DependencyProperty EndAddressProperty =
            DependencyProperty.Register("EndAddress", typeof(string), typeof(AutoDetect), new PropertyMetadata(null));


        public int StartPort
        {
            get { return (int)GetValue(StartPortProperty); }
            set { SetValue(StartPortProperty, value); }
        }

        public static readonly DependencyProperty StartPortProperty =
            DependencyProperty.Register("StartPort", typeof(int), typeof(AutoDetect), new PropertyMetadata(5000));


        public int EndPort
        {
            get { return (int)GetValue(EndPortProperty); }
            set { SetValue(EndPortProperty, value); }
        }

        public static readonly DependencyProperty EndPortProperty =
            DependencyProperty.Register("EndPort", typeof(int), typeof(AutoDetect), new PropertyMetadata(5020));


        public int PingTimeout
        {
            get { return (int)GetValue(PingTimeoutProperty); }
            set { SetValue(PingTimeoutProperty, value); }
        }

        public static readonly DependencyProperty PingTimeoutProperty =
            DependencyProperty.Register("PingTimeout", typeof(int), typeof(AutoDetect), new PropertyMetadata(500));

        #endregion

        const System.Windows.Threading.DispatcherPriority PRIORITY_BACKGROUND = System.Windows.Threading.DispatcherPriority.Background;

        private ObservableCollection<DeviceInfo> _deviceInfos;
        public ObservableCollection<DeviceInfo> DeviceInfos
        {
            get
            {
                if (_deviceInfos == null) _deviceInfos = new ObservableCollection<DeviceInfo>();
                return _deviceInfos;
            }
            set
            {
                _deviceInfos = value;
            }
        }

        #region "Find Devices"

        int portRangeStart = 5000;
        int portRangeStop = 5020;

        int pingTimeout = 500;

        List<IPAddress> addressRange;

        TrakHound.MTConnectSniffer.Sniffer sniffer;


        public void FindDevices()
        {
            DevicesLoading = true;
            DevicesAlreadyAdded = 0;
            DevicesNotAdded = 0;
            DeviceInfos.Clear();

            pingTimeout = PingTimeout;
            var addressRange = GetAddressRange();
            var portRange = GetPortRange();

            ThreadPool.QueueUserWorkItem((o) =>
            {
                // Create an MTConnect Sniffer
                sniffer = new TrakHound.MTConnectSniffer.Sniffer();
                sniffer.DeviceFound += Sniffer_DeviceFound;
                sniffer.RequestsCompleted += Sniffer_RequestsCompleted;
                sniffer.PingSent += Sniffer_PingSent;
                sniffer.PingReceived += Sniffer_PingReceived;
                sniffer.PortClosed += Sniffer_PortClosed;
                sniffer.PortOpened += Sniffer_PortOpened;
                sniffer.ProbeSent += Sniffer_ProbeSent;
                sniffer.ProbeError += Sniffer_ProbeError;
                sniffer.ProbeSuccessful += Sniffer_ProbeSuccessful;
                sniffer.Timeout = pingTimeout;
                sniffer.AddressRange = addressRange;
                sniffer.PortRange = portRange;
                sniffer.Start();
            });
        }

        private void Sniffer_ProbeSuccessful(IPAddress address, int port)
        {
            AddtoLog(LogType.PROBE, string.Format("MTConnect Probe Successful @ {0}:{1}", address.ToString(), port));
        }

        private void Sniffer_ProbeError(IPAddress address, int port)
        {
            AddtoLog(LogType.PROBE, string.Format("MTConnect Probe Error @ {0}:{1}", address.ToString(), port));
        }

        private void Sniffer_ProbeSent(IPAddress address, int port)
        {
            AddtoLog(LogType.PROBE, string.Format("MTConnect Probe Sent to {0}:{1}", address.ToString(), port));
        }

        private void Sniffer_PortOpened(IPAddress address, int port)
        {
            AddtoLog(LogType.PORT, string.Format("Port Open @ {0}:{1}", address.ToString(), port));
        }

        private void Sniffer_PortClosed(IPAddress address, int port)
        {
            AddtoLog(LogType.PORT, string.Format("Port Closed @ {0}:{1}", address.ToString(), port));
        }

        private void Sniffer_PingReceived(IPAddress address, System.Net.NetworkInformation.PingReply reply)
        {
            AddtoLog(LogType.PING, string.Format("Ping Received @ {0}:{1} in {2}ms", address.ToString(), reply.Status, reply.RoundtripTime));
        }

        private void Sniffer_PingSent(IPAddress address)
        {
            AddtoLog(LogType.PORT, string.Format("Ping Sent to {0}", address.ToString()));
        }

        private IPAddress[] GetAddressRange()
        {
            var hostIp = Network_Functions.GetHostIP();
            var hostSubnet = Network_Functions.GetSubnetMask(hostIp);

            if (hostIp != null && hostSubnet != null)
            {
                var ipNetwork = IPNetwork.Parse(hostIp, hostSubnet);
                if (ipNetwork != null)
                {
                    var ips = IPNetwork.ListIPAddress(ipNetwork).ToList();
                    if (ips != null && ips.Count > 0)
                    {
                        // Get Start Address
                        IPAddress start = null;
                        IPAddress.TryParse(StartAddress, out start);

                        // Get End Address
                        IPAddress end = null;
                        IPAddress.TryParse(EndAddress, out end);

                        if (start != null && end != null)
                        {
                            var range = new Network_Functions.IPAddressRange(start, end);
                            addressRange = ips.FindAll(o => range.IsInRange(o));

                            // Set HostIp to LocalHost instead of using network IP
                            int localhostIndex = addressRange.FindIndex(o => o.ToString() == hostIp.ToString());
                            if (localhostIndex >= 0) addressRange[localhostIndex] = IPAddress.Loopback;

                            return addressRange.ToArray();
                        }
                    }
                }
            }

            return null;
        }

        private int[] GetPortRange()
        {
            var ips = new List<int>();

            int s = StartPort;
            int e = EndPort;
            for (var i = s; i <= ((e - s) + s); i++)
            {
                Console.WriteLine("Port = " + i);
                ips.Add(i);
            }



            return ips.ToArray();
        }


        private void Sniffer_RequestsCompleted(long milliseconds)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DevicesLoading = false;
            }));
        }

        private void Sniffer_DeviceFound(TrakHound.MTConnectSniffer.MTConnectDevice device)
        {
            if (device != null && device.IpAddress != null && !string.IsNullOrEmpty(device.DeviceName))
            {
                // Create Agent Url
                var protocol = "http://";
                var adr = device.IpAddress.ToString();
                if (adr.IndexOf(protocol) >= 0) adr = adr.Substring(protocol.Length);
                else adr = protocol + adr;
                var url = adr;
                if (device.Port > 0 && device.Port != 80) url += ":" + device.Port;

                // Run Probe Request
                var probe = new MTConnect.Clients.Probe(url, device.DeviceName);
                probe.Successful += Probe_Successful;
                probe.UserObject = device;
                probe.ExecuteAsync();
            }
        }

        private void Probe_Successful(MTConnect.MTConnectDevices.Document document)
        {
            try
            {
                var device = document.UserObject as TrakHound.MTConnectSniffer.MTConnectDevice;
                if (device != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (_lock)
                        {
                            if (Devices != null)
                            {
                                bool match = false;

                                // Check Device List to see if the Device has already been added
                                match = Devices.ToList().Exists(o =>
                                o.Agent != null &&
                                o.Agent.Address == device.IpAddress.ToString() &&
                                o.Agent.Port == device.Port &&
                                o.Agent.DeviceName == device.DeviceName);

                                // If Device is not already added then add it
                                if (!match && document.Devices.Count > 0)
                                {
                                    DevicesNotAdded++;

                                    var info = new DeviceInfo(device.IpAddress.ToString(), device.Port, document.Devices[0]);
                                    DeviceInfos.Add(info);
                                }
                                else DevicesAlreadyAdded++;
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background, new object[] { });
                }
            }
            catch (Exception ex) { logger.Error(ex); }
        }

        private class AgentConfiguration
        {
            public string Address { get; set; }
            public int Port { get; set; }
            public string DeviceName { get; set; }

            public static AgentConfiguration Read(DeviceConfiguration config)
            {
                var result = new AgentConfiguration();
                result.Address = XML_Functions.GetInnerText(config.Xml, "//Agent/Address");

                int port = 80;
                int.TryParse(XML_Functions.GetInnerText(config.Xml, "//Agent/Port"), out port);
                result.Port = port;

                result.DeviceName = XML_Functions.GetInnerText(config.Xml, "//Agent/DeviceName");

                return result;
            }
        }
        
        #endregion

        #region "Add Device"

        private void AddDevice(DeviceInfo info)
        {
            info.Loading = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(AddDevice_Worker), info);
        }

        private void AddDevice_Worker(object o)
        {
            var info = (DeviceInfo)o;

            bool success = false;

            if (info != null && info.Device != null)
            {
                var probeData = new Configuration.ProbeData();
                probeData.Address = info.Address;
                probeData.Port = info.Port.ToString();
                probeData.Device = info.Device;

                var config = Configuration.Create(probeData);

                // Add Device to user (or save to disk if local)
                if (currentUser != null) success = TrakHound.API.Devices.Update(currentUser, config);
                else success = DeviceConfiguration.Save(config);

                if (success)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Send message that device was added
                        var data = new EventData(this);
                        data.Id = "DEVICE_ADDED";
                        data.Data01 = new DeviceDescription(config);
                        SendData?.Invoke(data);

                        int i = DeviceInfos.ToList().FindIndex(x => x.Id == info.Id);
                        if (i >= 0) DeviceInfos.RemoveAt(i);

                        // Increment counters
                        DevicesNotAdded = DeviceInfos.Count;
                        DevicesAlreadyAdded += 1;
                    }));
                }
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                info.Loading = false;

            }), System.Windows.Threading.DispatcherPriority.Background, new object[] { });
        }

        #endregion
        
        #region "Buttons"

        private void Add_Clicked(TrakHound_UI.Button bt)
        {
            if (bt.DataObject != null)
            {
                var info = (DeviceInfo)bt.DataObject;
                if (!info.Loading) AddDevice(info);
            }
        }

        private void Search_Clicked(TrakHound_UI.Button bt)
        {
            FindDevices();
        }

        private void AddAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var info in DeviceInfos)
            {
                AddDevice(info);
            }
        }

        #endregion

        #region "Details Log"

        private ObservableCollection<LogItem> _logItems;
        public ObservableCollection<LogItem> LogItems
        {
            get
            {
                if (_logItems == null)
                {
                    _logItems = new ObservableCollection<LogItem>();
                }
                return _logItems;
            }
            set
            {
                _logItems = value;
            }
        }

        public enum LogType
        {
            INFO,
            PING,
            PORT,
            PROBE
        }

        public class LogItem : INotifyPropertyChanged
        {
            public LogItem(LogType type, string text)
            {
                Type = type;
                Text = text;
            }

            private LogType _type;
            public LogType Type
            {
                get { return _type; }
                set { SetField(ref _type, value, "Type"); }
            }

            private string _text;
            public string Text
            {
                get { return _text; }
                set { SetField(ref _text, value, "Text"); }
            }

            private bool _shown = true;
            public bool Shown
            {
                get { return _shown; }
                set { SetField(ref _shown, value, "Shown"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetField<T>(ref T field, T value, string propertyName)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }

        private void AddtoLog(LogType type, string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                bool shown = false;

                switch (type)
                {
                    case LogType.PING: shown = DisplayPingLog; break;
                    case LogType.PORT: shown = DisplayPortLog; break;
                    case LogType.PROBE: shown = DisplayProbeLog; break;
                    default: shown = true; break;
                }

                var item = new LogItem(type, line);
                item.Shown = shown;

                LogItems.Add(item);           

            }), System.Windows.Threading.DispatcherPriority.Background, new object[] { });
        }


        public bool DisplayPingLog
        {
            get { return (bool)GetValue(DisplayPingLogProperty); }
            set { SetValue(DisplayPingLogProperty, value); }
        }

        public static readonly DependencyProperty DisplayPingLogProperty =
            DependencyProperty.Register("DisplayPingLog", typeof(bool), typeof(AutoDetect), new PropertyMetadata(true));

        public bool DisplayPortLog
        {
            get { return (bool)GetValue(DisplayPortLogProperty); }
            set { SetValue(DisplayPortLogProperty, value); }
        }

        public static readonly DependencyProperty DisplayPortLogProperty =
            DependencyProperty.Register("DisplayPortLog", typeof(bool), typeof(AutoDetect), new PropertyMetadata(true));

        public bool DisplayProbeLog
        {
            get { return (bool)GetValue(DisplayProbeLogProperty); }
            set { SetValue(DisplayProbeLogProperty, value); }
        }

        public static readonly DependencyProperty DisplayProbeLogProperty =
            DependencyProperty.Register("DisplayProbeLog", typeof(bool), typeof(AutoDetect), new PropertyMetadata(true));


        private void FilterLogItems()
        {
            lock(LogItems)
            {
                foreach (var logItem in LogItems)
                {
                    switch (logItem.Type)
                    {
                        case LogType.PING: logItem.Shown = DisplayPingLog; break;
                        case LogType.PORT: logItem.Shown = DisplayPortLog; break;
                        case LogType.PROBE: logItem.Shown = DisplayProbeLog; break;
                    }
                }
            }
        }

        private void LogFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            FilterLogItems();
        }

        #endregion

        private void DeviceManager_Clicked(TrakHound_UI.Button bt) { if (ParentPage != null) ParentPage.OpenDeviceList(); }

        private void AddDevicesManually_Clicked(TrakHound_UI.Button bt) { if (ParentPage != null) ParentPage.ShowManual(); }

        private void ResetDefault_Clicked(TrakHound_UI.Button bt)
        {
            PingTimeout = 500;
            StartPort = 5000;
            EndPort = 5020;

            SetDefaultAddressRange();
        }

        private void SetDefaultAddressRange()
        {
            var hostIp = Network_Functions.GetHostIP();
            var hostSubnet = Network_Functions.GetSubnetMask(hostIp);

            if (hostIp != null && hostSubnet != null)
            {
                var ipNetwork = IPNetwork.Parse(hostIp, hostSubnet);
                if (ipNetwork != null)
                {
                    var ips = IPNetwork.ListIPAddress(ipNetwork).ToList();
                    if (ips != null && ips.Count > 0)
                    {
                        StartAddress = ips[0].ToString();
                        EndAddress = ips[ips.Count - 1].ToString();
                    }
                }
            }
        }

        private void Cancel_Clicked(TrakHound_UI.Button bt)
        {
            Cancel();
        }

        private void Cancel()
        {
            DevicesLoading = false;
        }

    }
}
