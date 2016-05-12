﻿// Copyright (c) 2016 Feenux LLC, All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using System.IO;
using System.Text;

using TH_Global;
using TH_Global.Web;

namespace TH_GitHub
{

    public enum TokenReturnType
    {
        Created,
        Existing
    }

    public class OAuth2Token
    {
        public TokenReturnType ReturnType { get; set; }

        public string Id { get; set; }
        public string Url { get; set; }
        public string Token { get; set; }
        public string Token_Last_Eight { get; set; }
        public string Hashed_Token { get; set; }

        public static OAuth2Token Parse(string s)
        {
            OAuth2Token result = null;

            var serializer = new JsonSerializer();
            try
            {
                result = (OAuth2Token)serializer.Deserialize(new JsonTextReader(new StringReader(s)), typeof(OAuth2Token));
            }
            catch
            {
                Logger.Log("Error During GitHub OAuth2 Token JSON Parse : " + s, Logger.LogLineType.Error);
            }

            return result;
        }

        public static OAuth2Token Get(Authentication.Crendentials credentials)
        {
            string format = "\"client_secret\": \"{0}\", \"scopes\": [ \"repo\" ], \"note\": \"{1}\"";

            string client_id = "a1178c3ffdfd1adea560";
            string client_secret = "acf7e9e80eab5f238271ad8a2e0863025ad326ba";
            string note = "TrakHound";
            string note_url = "http://www.trakhound.org";

            string data = string.Format(format, client_secret, note, note_url);
            data = "{" + data + "}";

            byte[] bytes = Encoding.UTF8.GetBytes(data);

            var headers = new HTTP.HeaderData[1];
            headers[0] = Authentication.GetBasicHeader(credentials);

            string response = HTTP.PUT(Authentication.AUTHENTICATION_URL + "/clients/" + client_id, bytes, headers, "TrakHound");

            return Parse(response);
        }
    }

}
