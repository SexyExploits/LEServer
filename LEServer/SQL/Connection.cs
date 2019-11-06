using System;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Collections.Generic;

namespace LE {
    class DbConnection {
        public static MySqlConnection SetupConnection() {
            string Host = "149.248.59.77";
            string Database = "LiveEmulation";
            string Username = "LiveEmulation";
            string Pass = "?s5F-9<xC5t/yU!)";
            string Port = "3306";
            return new MySqlConnection($"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Pass};");
        }

        public static void Connect(MySqlConnection Con) {
            try {
                Con.Open();
            } catch (MySqlException Ex) {
                List<Log.PrintQueue> MySqlOpenConId = Log.GetQueue();
                Log.Add(MySqlOpenConId, ConsoleColor.Red, "MySql Con Error", null);
                Log.Add(MySqlOpenConId, ConsoleColor.Red, "Exception Message:", Ex.Message);
                Log.Add(MySqlOpenConId, ConsoleColor.Red, "StrackTrace:", Ex.StackTrace);
                Log.Print(MySqlOpenConId);
                Thread.Sleep(7000);
                Utilities.RestartServer();
            }
        }

    }
}
