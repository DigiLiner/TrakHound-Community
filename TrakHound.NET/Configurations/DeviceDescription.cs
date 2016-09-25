﻿// Copyright (c) 2016 Feenux LLC, All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using TrakHound.API;

namespace TrakHound.Configurations
{
    public class DeviceDescription
    {
        public DeviceDescription() { }

        public DeviceDescription(Data.DeviceInfo deviceInfo)
        {
            UniqueId = deviceInfo.UniqueId;
            Enabled = deviceInfo.Enabled;
            Description = deviceInfo.Description;
            Agent = deviceInfo.Agent;
        }

        public DeviceDescription(DeviceConfiguration deviceConfig)
        {
            UniqueId = deviceConfig.UniqueId;
            Enabled = deviceConfig.Enabled;
            Index = deviceConfig.Index;
            Description = deviceConfig.Description;
            Agent = deviceConfig.Agent;
        }


        public string UniqueId { get; set; }

        public bool Enabled { get; set; }

        public int Index { get; set; }

        public Data.DescriptionInfo Description { get; set; }

        public Data.AgentInfo Agent { get; set; }
    }
}