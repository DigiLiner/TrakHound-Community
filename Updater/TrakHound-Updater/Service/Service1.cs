﻿// Copyright (c) 2016 Feenux LLC, All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ServiceProcess;

using TH_Global;
using TH_Global.Functions;
using TH_Global.Updates;
using System.IO;

namespace TrakHound_Updater
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            var status = new ServiceStatus();
            status.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            status.dwWaitHint = 10000;
            SetServiceStatus(this.ServiceHandle, ref status);

            Start();

            // Update the service state to Running.
            status.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref status);
        }

        protected override void OnStop()
        {
            var status = new ServiceStatus();
            status.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            status.dwWaitHint = 10000;
            SetServiceStatus(this.ServiceHandle, ref status);

            Stop();

            // Update the service state to Stopped.
            status.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref status);
        }

        #region "Service Status"

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public long dwServiceType;
            public ServiceState dwCurrentState;
            public long dwControlsAccepted;
            public long dwWin32ExitCode;
            public long dwServiceSpecificExitCode;
            public long dwCheckPoint;
            public long dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        #endregion


        public void Start()
        {
            ReadConfigFile();
        }

        public void Stop()
        {
            StopUpdates();
        }


        private void ReadConfigFile()
        {
            // Read the update_config.xml file
            configuration = UpdateConfiguration.Read();

            if (configuration.ClearUpdateQueue)
            {
                Update.ClearAll();
            }

            if (configuration.ApplyNow)
            {
                AppInfo[] infos = GetUpdates();
                ApplyUpdates(infos);
            }
            else if (configuration.CheckNow)
            {
                GetUpdates();
            }

            configuration.ApplyNow = false;
            configuration.CheckNow = false;
            configuration.ClearUpdateQueue = false;
            StopConfigurationFileWatcher();
            UpdateConfiguration.Create(configuration);

            // Monitor update_config.xml file for any changes
            StartConfigurationFileWatcher();

            // If updates are enabled then start auto check timer
            if (configuration.Enabled) StartUpdates();
            else Logger.Log("Auto Updates Disabled", Logger.LogLineType.Notification);
        }


        private System.Timers.Timer updateTimer;

        private void StartUpdates()
        {
            Logger.Log("TrakHound-Updater Started!", Logger.LogLineType.Notification);

            StopUpdates();

            updateTimer = new System.Timers.Timer();
            updateTimer.Interval = GetMillisecondsFromMinutes(configuration.UpdateCheckInterval);
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Enabled = true;
        }

        private void StopUpdates()
        {
            if (updateTimer != null) updateTimer.Enabled = false;
        }

        private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var timer = (System.Timers.Timer)sender;

            timer.Enabled = false;

            // Get available updates, download, store links in Registry
            AppInfo[] infos = GetUpdates();

            ApplyUpdates(infos);

            timer.Interval = GetMillisecondsFromMinutes(configuration.UpdateCheckInterval);
            timer.Enabled = true;

            Logger.Log("Update Timer : Interval = " + timer.Interval.ToString() + "ms", Logger.LogLineType.Debug);
        }


        private AppInfo[] GetUpdates()
        {
            var infos = new List<AppInfo>();

            //string[] names = Registry.GetValueNames("Update_Urls");
            string[] names = Registry_Functions.GetKeyNames();
            if (names != null)
            {
                foreach (var name in names)
                {
                    string url = Registry_Functions.GetValue(Update.UPDATE_URL, name);
                    if (url != null) infos.Add(Update.Get(url));
                }
            }

            return infos.ToArray();
        }

        private void ApplyUpdates(AppInfo[] infos)
        {
            if (infos != null)
            {
                foreach (var info in infos)
                {
                    Update.Apply(info);
                }
            }
        }

        private UpdateConfiguration configuration;

        private FileSystemWatcher watcher;

        private void StartConfigurationFileWatcher()
        {
            StopConfigurationFileWatcher();

            watcher = new FileSystemWatcher(FileLocations.TrakHound, UpdateConfiguration.CONFIG_FILENAME);
            watcher.Changed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void StopConfigurationFileWatcher()
        {
            if (watcher != null) watcher.EnableRaisingEvents = false;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            ReadConfigFile();
        }

        private static double GetMillisecondsFromMinutes(double minutes)
        {
            var ts = TimeSpan.FromMinutes(minutes);
            return ts.TotalMilliseconds;
        }

    }
}
