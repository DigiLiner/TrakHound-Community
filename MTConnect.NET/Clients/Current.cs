﻿// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Net;
using System.Net.Http;

namespace MTConnect.Clients
{
    public class Current
    {
        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        public Current()
        {
            At = -1;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        public Current(string baseUrl)
        {
            BaseUrl = baseUrl;
            At = -1;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        /// <param name="deviceName">The name of the requested device</param>
        public Current(string baseUrl, string deviceName)
        {
            BaseUrl = baseUrl;
            DeviceName = deviceName;
            At = -1;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        /// <param name="atSequence">The sequence number to retrieve the current data for</param>
        public Current(string baseUrl, long atSequence)
        {
            BaseUrl = baseUrl;
            At = atSequence;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        /// <param name="deviceName">The name of the requested device</param>
        /// <param name="atSequence">The sequence number to retrieve the current data for</param>
        public Current(string baseUrl, string deviceName, long atSequence)
        {
            BaseUrl = baseUrl;
            DeviceName = deviceName;
            At = atSequence;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        /// <param name="deviceName">The name of the requested device</param>
        /// <param name="path">The XPath expression specifying the components and/or data items to include</param>
        public Current(string baseUrl, string deviceName, string path)
        {
            BaseUrl = baseUrl;
            DeviceName = deviceName;
            At = -1;
            Path = path;
        }

        /// <summary>
        /// Create a new Current Request Client
        /// </summary>
        /// <param name="baseUrl">The base URL for the Current Request</param>
        /// <param name="deviceName">The name of the requested device</param>
        /// <param name="atSequence">The sequence number to retrieve the current data for</param>
        /// <param name="path">The XPath expression specifying the components and/or data items to include</param>
        public Current(string baseUrl, string deviceName, string path, long atSequence)
        {
            BaseUrl = baseUrl;
            DeviceName = deviceName;
            At = atSequence;
            Path = path;
        }

        /// <summary>
        /// The base URL for the Current Request
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// (Optional) The name of the requested device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary> 
        /// (Optional) The sequence number to retrieve the current data for
        /// </summary>
        public long At { get; set; }

        /// <summary>
        /// (Optional) The XPath expression specifying the components and/or data items to include
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// User settable object sent with request and returned in Document on response
        /// </summary>
        public object UserObject { get; set; }

        /// <summary>
        /// Raised when an MTConnectError Document is received
        /// </summary>
        public event MTConnectErrorHandler Error;

        /// <summary>
        /// Raised when an Connection Error occurs
        /// </summary>
        public event ConnectionErrorHandler ConnectionError;

        /// <summary>
        /// Raised when an MTConnectStreams Document is received successfully
        /// </summary>
        public event MTConnectStreamsHandler Successful;


        /// <summary>
        /// Execute the Current Request Synchronously
        /// </summary>
        public MTConnectStreams.Document Execute()
        {
            //// Create HTTP Client and Request Data
            //var client = new RestClient();
            //client.Timeout = 2000;
            //client.ReadWriteTimeout = 2000;
            //IRestResponse response = client.Execute(CreateRequest());
            //return ProcessResponse(response);

            try
            {
                var uri = CreateUri();

                //// Create HTTP Client and Request Data
                var client = new HttpClient();
                return ProcessResponse(client.GetStringAsync(uri).Result);
            }
            catch (Exception ex) { }

            return null;
        }

        ///// <summary>
        ///// Execute the Current Request Asynchronously
        ///// </summary>
        //public void ExecuteAsync()
        //{
        //    // Create HTTP Client and Request Data
        //    var client = new RestClient(CreateUri());
        //    client.ExecuteAsync(CreateRequest(), AsyncCallback);
        //}

        private string CreateUri()
        {
            var url = BaseUrl;
            if (!string.IsNullOrEmpty(DeviceName)) url = CombineUrl(url, DeviceName);
            url = CombineUrl(url, "current");
            return url;

            //// Create Uri
            //var uri = new Uri(BaseUrl);
            //if (DeviceName != null) uri = new Uri(uri, DeviceName);
            //uri = new Uri(uri, "current");
            //return uri.ToString();
        }

        public static string CombineUrl(string baseUrl, string path)
        {
            if (baseUrl == null || baseUrl.Length == 0)
            {
                return baseUrl;
            }

            if (path.Length == 0)
            {
                return path;
            }

            baseUrl = baseUrl.TrimEnd('/', '\\');
            path = path.TrimStart('/', '\\');

            return $"{baseUrl}/{path}";
        }

        //private RestRequest CreateRequest()
        //{
        //    var request = new RestRequest(Method.GET);

        //    // Add 'At' parameter
        //    if (At > -1) request.AddQueryParameter("at", At.ToString());

        //    // Add 'Path' parameter
        //    if (!string.IsNullOrEmpty(Path)) request.AddQueryParameter("path", Path);

        //    return request;
        //}

        private MTConnectStreams.Document ProcessResponse(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                string xml = response;

                // Process MTConnectStreams Document
                var doc = MTConnectStreams.Document.Create(xml);
                if (doc != null)
                {
                    return doc;
                }
                else
                {
                    // Process MTConnectError Document (if MTConnectDevices fails)
                    var errorDoc = MTConnectError.Document.Create(xml);
                    if (errorDoc != null) Error?.Invoke(errorDoc);
                }
            }

            return null;
        }

        //private void AsyncCallback(IRestResponse response)
        //{
        //    var doc = ProcessResponse(response);
        //    if (doc != null) Successful?.Invoke(doc);
        //}
    }
}
