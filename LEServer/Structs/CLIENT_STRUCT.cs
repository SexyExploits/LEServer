﻿using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;

namespace LE {
    public class Discord {
        //{"id": "400734527020072980", "popup": "false", "token": "LE-1234", "primary": "true", "verified": "true"}
        public string id { get; set; }
        public bool primary { get; set; }
        public bool verified { get; set; }
        public bool popup { get; set; }
        public string token { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CLIENT_STRUCT {
        public int id;
        public string name;
        public string cpukey;
        public string gamertag;
        public string consoletype;
        public int challengesran;
        public string ui_colors;
        public string sessiontoken;
        public string ip;
        public string titleid;
        public string kvserial;
        public bool online;
        public string mapcordinates;
        public string bannedreason;
        public string discord;

        public DateTime time;
        public DateTime kvfirstunbanned;
        public DateTime lastloginTime;

        public CLIENT_ACTION consoleaction;
        public CLIENT_ACTION_COMPLETED actioncompleted;
        public CLIENT_KVSTATUS kvstatus;
        public CLIENT_AUTHSTATUS authstatus;
    }

    public struct ClientMetric {
        public int m_Index;
        public int m_Type;

        public ClientMetric(int index, int type) {
            m_Index = index;
            m_Type = type;
        }
    }
}
