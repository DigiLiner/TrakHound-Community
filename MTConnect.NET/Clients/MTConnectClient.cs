﻿// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Threading;

namespace MTConnect.Clients
{
    public class MTConnectClient
    {
        public MTConnectClient()
        {
            Init();
        }

        public MTConnectClient(string baseUrl)
        {
            Init();
            BaseUrl = baseUrl;
        }

        public MTConnectClient(string baseUrl, string deviceName)
        {
            Init();
            BaseUrl = baseUrl;
            DeviceName = deviceName;
        }

        private void Init()
        {
            Interval = 500;
            MaximumSampleCount = 200;
        }

        public string BaseUrl { get; set; }

        public string DeviceName { get; set; }

        public int Interval { get; set; }

        public long MaximumSampleCount { get; set; }

        public event MTConnectDevicesHandler ProbeReceived;
        public event MTConnectStreamsHandler CurrentReceived;
        public event MTConnectStreamsHandler SampleReceived;
        public event MTConnectErrorHandler Error;
        public event ConnectionErrorHandler ConnectionError;
        public event XmlHandler XmlError;

        public event StreamStatusHandler Started;
        public event StreamStatusHandler Stopped;

        private Thread thread;
        private ManualResetEvent stop;

        private Stream sampleStream;

        private SequenceRange _sampleRange;
        public  SequenceRange SampleRange
        {
            get
            {
                if (_sampleRange == null) _sampleRange = new SequenceRange(0, 0);
                return _sampleRange;
            }
        }

        public void Start()
        {
            Started?.Invoke();

            stop = new ManualResetEvent(false);

            thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {
            if (stop != null) stop.Set();

            if (sampleStream != null) sampleStream.Stop();

            try
            {
                thread.Abort();
            }
            catch { }
        }

        private void Worker()
        {
            long instanceId = -1;
            bool initialize = true;

            do
            {
                var probe = new Probe(BaseUrl, DeviceName);
                probe.Error += MTConnectErrorRecieved;
                probe.ConnectionError += ProcessConnectionError;
                var probeDoc = probe.Execute();
                if (probeDoc != null)
                {
                    // Raise ProbeReceived Event
                    ProbeReceived?.Invoke(probeDoc);

                    do
                    {
                        var current = new Current(BaseUrl, DeviceName);
                        current.Error += MTConnectErrorRecieved;
                        current.ConnectionError += ProcessConnectionError;
                        var currentDoc = current.Execute();
                        if (currentDoc != null)
                        {
                            // Check if FirstSequence is larger than previously Sampled
                            if (!initialize) initialize = SampleRange.From > 0 && currentDoc.Header.FirstSequence > SampleRange.From;

                            // Raise CurrentReceived Event
                            if (initialize) CurrentReceived?.Invoke(currentDoc);

                            // Check if Agent InstanceID has changed (Agent has been reset)
                            if (initialize || instanceId != currentDoc.Header.InstanceId)
                            {
                                SampleRange.Reset();
                                instanceId = currentDoc.Header.InstanceId;

                                // Restart entire request sequence if new Agent Instance Id is read (probe could have changed)
                                if (!initialize) break;
                            }

                            long from;
                            if (initialize) from = currentDoc.Header.NextSequence;
                            else
                            {
                                // If recovering from Error then use last Sample Range that was sampled successfully
                                // Try to get Buffer minus 100 (to account for time between current and sample requests)
                                from = currentDoc.Header.LastSequence - (currentDoc.Header.BufferSize - 100);
                                from = Math.Max(from, currentDoc.Header.FirstSequence);
                                from = Math.Max(SampleRange.From, from);
                            }

                            long to;
                            if (initialize) to = from;
                            else
                            {
                                // Get up to the MaximumSampleCount
                                to = currentDoc.Header.NextSequence;
                                to = Math.Min(to, from + MaximumSampleCount);
                            }

                            // Set the SampleRange for subsequent samples
                            SampleRange.From = from;
                            SampleRange.To = to;

                            initialize = false;

                            // Create the Url to use for the Sample Stream
                            string url = CreateSampleUrl(BaseUrl, DeviceName, Interval, from, MaximumSampleCount);

                            // Create and Start the Sample Stream
                            sampleStream = new Stream(url);
                            sampleStream.XmlReceived += ProcessSampleResponse;
                            sampleStream.XmlError += SampleStream_XmlError;
                            sampleStream.ConnectionError += ProcessConnectionError;
                            sampleStream.Start();
                        }
                    } while (!stop.WaitOne(5000, true));
                }
            } while (!stop.WaitOne(5000, true));

            Stopped?.Invoke();
        }

        private void SampleStream_XmlError(string xml)
        {
            XmlError?.Invoke(xml);
        }

        private void ProcessSampleResponse(string xml)
        {
            if (!string.IsNullOrEmpty(xml))
            {
                // Process MTConnectStreams Document
                var doc = MTConnectStreams.Document.Create(xml);
                if (doc != null)
                {
                    int itemCount = -1;
                    if (doc.DeviceStreams.Count > 0)
                    {
                        var deviceStream = doc.DeviceStreams.Find(o => o.Name == DeviceName);
                        if (deviceStream != null)
                        {
                            // Get number of DataItems returned by Sample
                            itemCount = deviceStream.DataItems.Count;

                            var debugRange = new SequenceRange(SampleRange.From, itemCount + SampleRange.From);

                            SampleRange.From += itemCount;
                            SampleRange.To = doc.Header.NextSequence;

                            SampleReceived?.Invoke(doc);
                        }                       
                    } 
                }
                else
                {
                    // Process MTConnectError Document (if MTConnectDevices fails)
                    var errorDoc = MTConnectError.Document.Create(xml);
                    if (errorDoc != null) Error?.Invoke(errorDoc);
                }
            }
        }

        private void MTConnectErrorRecieved(MTConnectError.Document errorDocument)
        {
            Error?.Invoke(errorDocument);
        }

        private void ProcessConnectionError(Exception ex)
        {
            ConnectionError?.Invoke(ex);
        }

        private static string CreateSampleUrl(string baseUrl, string deviceName, int interval, long from , long count)
        {
            var uri = new Uri(baseUrl);
            if (!string.IsNullOrEmpty(deviceName)) uri = new Uri(uri, deviceName);
            uri = new Uri(uri, "sample");
            var format = "{0}?from={1}&count={2}&interval={3}";

            return string.Format(format, uri, from, count, interval);
        }
    }
}
