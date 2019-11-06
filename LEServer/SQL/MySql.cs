using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace LE {
    class MySql {

        public static int FetchKvUsedOn(CLIENT_STRUCT ClientObj) {
            int ConsolesFoundOnKv = 0;
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.Parameters.AddWithValue("@kvserial", ClientObj.kvserial);
                    Cmd.CommandText = string.Format("SELECT COUNT(*) FROM clients WHERE kvserial=@kvserial");
                    ConsolesFoundOnKv = Convert.ToInt32(Cmd.ExecuteScalar());
                }
                DbCon.Close();
            }
            return ConsolesFoundOnKv;
        }

        public static void UpdateFirstUnbanned(CLIENT_STRUCT ClientObj) {
            if (ClientObj.kvstatus == CLIENT_KVSTATUS.UNBANNED && ClientObj.kvfirstunbanned.Year == 2009 && (int)ClientObj.authstatus >= 3 || LEServer.Freemode) {
                using (var DbCon = DbConnection.SetupConnection()) {
                    DbConnection.Connect(DbCon);
                    using (var Cmd = DbCon.CreateCommand()) {
                        Cmd.CommandText = string.Format("UPDATE clients SET kvfirst_unbanned=@kvfirst_unbanned, kvserial=@kvserial WHERE cpukey=@cpukey");
                        Cmd.Parameters.AddWithValue("@cpukey", ClientObj.cpukey);
                        Cmd.Parameters.AddWithValue("@kvfirst_unbanned", DateTime.Now);
                        Cmd.Parameters.AddWithValue("@kvserial", ClientObj.kvserial);
                        Cmd.ExecuteNonQuery();

                        DbCon.Close();
                        Cmd.Dispose();
                        DbCon.Dispose();
                    }
                }
            }
        }

        public static void UpdateKvThread(CLIENT_STRUCT ClientObj, bool ResetUnbanTime = false) {
            if (ResetUnbanTime) {
                using (var DbCon = DbConnection.SetupConnection()) {
                    DbConnection.Connect(DbCon);
                    using (var Cmd = DbCon.CreateCommand()) {
                        Cmd.CommandText = string.Format("UPDATE clients SET kvfirst_unbanned=@kvfirst_unbanned WHERE cpukey=@CpuKey");
                        DateTime resetunbantime = new DateTime(2009, 07, 08, 02, 30, 30);
                        Cmd.Parameters.AddWithValue("@kvfirst_unbanned", resetunbantime);
                        Cmd.Parameters.AddWithValue("@CpuKey", ClientObj.cpukey);
                        Cmd.ExecuteNonQuery();

                        DbCon.Close();
                        Cmd.Dispose();
                        DbCon.Dispose();
                    }
                }
            }

            if (ClientObj.kvstatus == CLIENT_KVSTATUS.UNBANNED) {
                if ((int)ClientObj.authstatus >= 3 || LEServer.Freemode) {
                    if (ClientObj.kvfirstunbanned.Year == 2009) 
                        UpdateFirstUnbanned(ClientObj);
                }
            } else if (ClientObj.kvstatus == CLIENT_KVSTATUS.BANNED) {
                using (var DbCon = DbConnection.SetupConnection()) {
                    DbConnection.Connect(DbCon);
                    using (var Cmd = DbCon.CreateCommand()) {
                        Cmd.CommandText = string.Format("UPDATE clients SET kvfirst_unbanned=@kvfirst_unbanned WHERE cpukey=@CpuKey");
                        DateTime resetunbantime = new DateTime(2009, 07, 08, 02, 30, 30);
                        Cmd.Parameters.AddWithValue("@kvfirst_unbanned", resetunbantime);
                        Cmd.Parameters.AddWithValue("@CpuKey", ClientObj.cpukey);

                        Cmd.ExecuteNonQuery();
                        DbCon.Close();
                        Cmd.Dispose();
                        DbCon.Dispose();
                    }
                }
            }
        }

        public static TOKEN_STRUCT GetToken(string Token) {
            TOKEN_STRUCT TokenObj = new TOKEN_STRUCT();

            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandText = "SELECT * FROM tokens WHERE token=@token LIMIT 1";
                    Cmd.Parameters.AddWithValue("@token", Token);
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read()) {
                            TokenObj.Token = (string)Results["token"];
                            TokenObj.Days = (int)Results["days"];
                            TokenObj.Status = (int)Results["status"];
                        }
                }
                DbCon.Close();
            }
            return TokenObj;
        }

        public static bool isUsed(string Token) {
            TOKEN_STRUCT TokenObj = GetToken(Token);
            return (TokenObj.Status == 1) ? true : false;
        }

        public static bool isBlackListed(string Token) {
            TOKEN_STRUCT TokenObj = GetToken(Token);
            return (TokenObj.Status == -1) ? true : false;
        }

        public static bool isValid(ref TOKEN_STRUCT TokenObj, string Token) {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {

                    Cmd.CommandText = "SELECT * FROM tokens WHERE token=@token LIMIT 1";
                    Cmd.Parameters.AddWithValue("@token", Token);
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read()) {
                            TokenObj.Token = (string)Results["token"];
                            TokenObj.Days = (int)Results["days"];
                            TokenObj.Status = (int)Results["status"];
                            return true;
                        }
                }
                DbCon.Close();
            }
            return false;
        }

        public static bool Redeem(TOKEN_STRUCT TokenObj, ref CLIENT_STRUCT ClientObj) {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {

                    // get gen_by value from token
                    string Gen_by = "";
                    Cmd.CommandText = "SELECT gen_by FROM tokens WHERE token=@tok";
                    Cmd.Parameters.AddWithValue("@tok", TokenObj.Token);
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read()) {
                            Gen_by = (string)Results["gen_by"];
                        } else {
                            Log.ErrorReportingPrint("Redeem Errror Selecting token !");
                            return false;
                        }

                    Cmd.ExecuteNonQuery();

                    // get the current staff gross value to add on to it
                    double CurrentGrossValue = 0;
                    Cmd.CommandText = "SELECT * FROM staff WHERE username=@Gen_by";
                    Cmd.Parameters.AddWithValue("@Gen_by", Gen_by);
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read()) {
                            CurrentGrossValue = (double)Results["gross"];
                        } else {
                            Log.ErrorReportingPrint("Redeem error regarding staff username!");
                            return false;
                        }
                    Cmd.ExecuteNonQuery();
                    
                    // update token status
                    Cmd.CommandText = "UPDATE tokens SET status=@status, dateused=@dateused, redeemed_by=@redeemed_by WHERE token=@token";
                    Cmd.Parameters.AddWithValue("@status", TOKEN_STATUS.ALREADYREDEEMED);
                    Cmd.Parameters.AddWithValue("@dateused", DateTime.Now);
                    Cmd.Parameters.AddWithValue("@redeemed_by", ClientObj.cpukey);
                    Cmd.Parameters.AddWithValue("@token", TokenObj.Token);
                    Cmd.ExecuteNonQuery();

                    // update client info
                    Cmd.CommandText = "UPDATE clients SET time=@time, auth_status=@auth_status WHERE cpukey=@cpukey";
                    Cmd.Parameters.AddWithValue("@cpukey", ClientObj.cpukey);

                    if (ClientObj.authstatus != CLIENT_AUTHSTATUS.BANNED)
                        Cmd.Parameters.AddWithValue("@auth_status", CLIENT_AUTHSTATUS.AUTHED);
                    else Cmd.Parameters.AddWithValue("@auth_status", CLIENT_AUTHSTATUS.BANNED);

                    if (ClientObj.time <= DateTime.Now)
                        Cmd.Parameters.AddWithValue("@time", DateTime.Now.AddDays(TokenObj.Days));
                    else Cmd.Parameters.AddWithValue("@time", ClientObj.time.AddDays(TokenObj.Days));
                    Cmd.ExecuteNonQuery();

                    // get the current prices from our server settings
                    double PriceOption1 = 0;
                    double PriceOption2 = 0;
                    double PriceOption3 = 0;
                    double PriceOption4 = 0;
                    double PriceOption5 = 0;

                    Cmd.CommandText = "SELECT * FROM serversettings";
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read()) {
                            JObject jobj = JObject.Parse((string)Results["pricing"]);
                            PriceOption1 = jobj["pricinglist1"].Value<double>();
                            PriceOption2 = jobj["pricinglist2"].Value<double>();
                            PriceOption3 = jobj["pricinglist3"].Value<double>();
                            PriceOption4 = jobj["pricinglist4"].Value<double>();
                            PriceOption5 = jobj["pricinglist5"].Value<double>();

                        } else {
                            Log.ErrorReportingPrint("Redeem error selecting data from serversettings !");
                            return false;
                        }
                    Cmd.ExecuteNonQuery();

                    // update our staff information
                    if (Gen_by != "" && PriceOption1 != 0 && PriceOption2 != 0 && PriceOption3 != 0 && PriceOption4 != 0) {
                        if (TokenObj.Days == 1)
                            CurrentGrossValue += PriceOption1;
                        else if (TokenObj.Days == 3)
                            CurrentGrossValue += PriceOption2;
                        else if (TokenObj.Days == 7)
                            CurrentGrossValue += PriceOption3;
                        else if (TokenObj.Days == 14)
                            CurrentGrossValue += PriceOption4;
                        else if (TokenObj.Days == 31)
                            CurrentGrossValue += PriceOption5;

                        Cmd.CommandText = "UPDATE staff SET gross=@Gross WHERE username=@GenBy";
                        Cmd.Parameters.AddWithValue("@GenBy", Gen_by);
                        Cmd.Parameters.AddWithValue("@Gross", CurrentGrossValue);
                        Cmd.ExecuteNonQuery();
                    } else {
                        Log.ErrorReportingPrint("Redeem error updating gross for staff member!");
                        return false;
                    }
                    DbCon.Close();
                    Cmd.Dispose();
                    DbCon.Dispose();
                }
                DbCon.Close();
            }
            return true;
        }

        public static bool GetClient(ref CLIENT_STRUCT ClientObj, string Key) {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandText = string.Format("SELECT * FROM clients WHERE (cpukey=@key OR sessionkey=@key)");
                    Cmd.Parameters.AddWithValue("@key", Key);
                    using (var Reader = Cmd.ExecuteReader())

                        if (Reader.Read()) {
                            ClientObj.sessiontoken = (string)Reader["sessionkey"];
                            ClientObj.cpukey = (string)Reader["cpukey"];
                            ClientObj.online = (bool)Reader["online"];
                            ClientObj.authstatus = (CLIENT_AUTHSTATUS)Reader["auth_status"];
                            ClientObj.ip = (string)Reader["ip"];
                            ClientObj.time = (DateTime)Reader["time"];
                            ClientObj.kvfirstunbanned = (DateTime)Reader["kvfirst_unbanned"];
                            ClientObj.lastloginTime = (DateTime)Reader["last_login"];
                            ClientObj.consoletype = (string)Reader["console_type"];
                            ClientObj.gamertag = (string)Reader["gamertag"];
                            ClientObj.mapcordinates = (string)Reader["map_cordinates"];
                            ClientObj.kvserial = (string)Reader["kvserial"];
                            ClientObj.consoleaction = (CLIENT_ACTION)Reader["console_action"];
                            ClientObj.actioncompleted = (CLIENT_ACTION_COMPLETED)Reader["action_completed"];
                            ClientObj.kvstatus = (CLIENT_KVSTATUS)Reader["kvstatus"];
                            ClientObj.bannedreason = (string)Reader["banned_reason"];
                            ClientObj.titleid = (string)Reader["titleid"];
                            ClientObj.challengesran = (int)Reader["challenges_ran"];
                            ClientObj.ui_colors = (string)Reader["ui_colors"];
                            DbCon.Close();
                            if (Countconsolesusingcpu(ClientObj) > 1)
                                BanClient(ClientObj);
                            return true;
                        } 
                }
                DbCon.Close();
            }
            return false;
        }

        public static void InsertClient(string Cpukey, string SessionKey) {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandText = "INSERT INTO clients (cpukey, auth_status, time, sessionkey, ui_colors, discord) VALUES (@cpukey, @auth_status, @time, @sessionkey, @ui_colors, @discord)";

                    Cmd.Parameters.AddWithValue("@CpuKey", Cpukey);
                    Cmd.Parameters.AddWithValue("@auth_status", CLIENT_AUTHSTATUS.NOTIME);
                    Cmd.Parameters.AddWithValue("@time", DateTime.Now);
                    Cmd.Parameters.AddWithValue("@sessionkey", SessionKey);

                    JObject UiColors = new JObject();
                    UiColors.Add("uicolorprimary", "0a7562");
                    UiColors.Add("uicoloronpress", "0fb391");
                    Cmd.Parameters.AddWithValue("@ui_colors", JsonConvert.SerializeObject(UiColors));

                    JObject DiscordObj = new JObject();
                    DiscordObj.Add("id", "0");
                    DiscordObj.Add("primary", "true");
                    DiscordObj.Add("verified", "false");
                    Cmd.Parameters.AddWithValue("@discord", JsonConvert.SerializeObject(DiscordObj));
                    Cmd.ExecuteNonQuery();
                }
                DbCon.Close();
            }
        }

        public static void UpdateServerSettingsThread() {
            while (true) {
                UpdateServerSettings();
                Thread.Sleep(300000); // every 5 min 300000
            }
        }
        
        public static void UpdateServerSettings() {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandText = "SELECT * FROM serversettings";
                    using (var Reader = Cmd.ExecuteReader())
                        if (Reader.Read()) {
                            LEServer.Freemode = (LEServer.DeveloperServer) ? true : (bool)Reader["freemode"];
                            LEServer.DebugModePrints = (LEServer.DeveloperServer) ? true : (bool)Reader["serverdebug"];
                            LEServer.ModuleChecks =  (bool)Reader["hashchecks"];
                            LEServer.UpdateXexData = !Reader.IsDBNull(Reader.GetOrdinal("updatedxex")) ? (byte[])Reader["updatedxex"] : null;
                            DbCon.Close();
                            return;
                        }
                     DbCon.Close();
                }
            }
        }

        public static void IncrementChallengeRuns() {
            int challengeruns = 0;
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandText = "SELECT * FROM serversettings";
                    using (var Results = Cmd.ExecuteReader())
                        if (Results.Read())
                            challengeruns = (int)Results["challengeruns"];
                    Cmd.ExecuteNonQuery();

                    Cmd.CommandText = string.Format("UPDATE serversettings SET challengeruns=@challs");
                    Cmd.Parameters.AddWithValue("@challs", challengeruns += 1);

                    Cmd.ExecuteNonQuery();
                    DbCon.Close();
                    Cmd.Dispose();
                    DbCon.Dispose();
                }
            }
        }

        public static void SaveClient(CLIENT_STRUCT ClientObj,  string KeyIdentifier) {
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {
                    Cmd.CommandTimeout = 10;
                    Cmd.CommandText = string.Format("UPDATE clients SET sessionkey=@sessionkey, time=@time, last_login=@last_login, online=@online, auth_status=@auth_status, kvstatus=@kvstatus, ip=@ip, titleid=@titleid, console_type=@console_type, gamertag=@gamertag, kvserial=@kvserial, map_cordinates=@map_cordinates, banned_reason=@banned_reason, challenges_ran=@challenges_ran, ui_colors=@ui_colors WHERE (cpukey=@key OR sessionkey=@key)");
                    Cmd.Parameters.AddWithValue("@cpukey", ClientObj.cpukey);
                    Cmd.Parameters.AddWithValue("@sessionkey", ClientObj.sessiontoken);
                    Cmd.Parameters.AddWithValue("@time", ClientObj.time);
                    Cmd.Parameters.AddWithValue("@last_login", DateTime.Now);
                    Cmd.Parameters.AddWithValue("@auth_status", ClientObj.authstatus);
                    Cmd.Parameters.AddWithValue("@kvstatus", ClientObj.kvstatus);
                    Cmd.Parameters.AddWithValue("@online", true);
                    Cmd.Parameters.AddWithValue("@ip", ClientObj.ip);
                    Cmd.Parameters.AddWithValue("@console_type", ClientObj.consoletype);
                    Cmd.Parameters.AddWithValue("@gamertag", ClientObj.gamertag);
                    Cmd.Parameters.AddWithValue("@kvserial", ClientObj.kvserial);
                    Cmd.Parameters.AddWithValue("@banned_reason", ClientObj.bannedreason);
                    Cmd.Parameters.AddWithValue("@challenges_ran", ClientObj.challengesran);
                    Cmd.Parameters.AddWithValue("@ui_colors", ClientObj.ui_colors);
              
                    if (ClientObj.titleid == null)
                        Cmd.Parameters.AddWithValue("@titleid", "Dashboard");
                    else
                        Cmd.Parameters.AddWithValue("@titleid", ClientObj.titleid);

                    if (ClientObj.mapcordinates == null)
                        Cmd.Parameters.AddWithValue("@map_cordinates", " ");
                    else
                        Cmd.Parameters.AddWithValue("@map_cordinates", ClientObj.mapcordinates);

                    Cmd.Parameters.AddWithValue("@key", KeyIdentifier);
                    
                    Cmd.ExecuteNonQuery();
                    DbCon.Close();
                    Cmd.Dispose();
                    DbCon.Dispose();
                }
            }
        }
        public static void BanClient(CLIENT_STRUCT ClientObj, string reason = null) {
            using (var dbcon = DbConnection.SetupConnection()) {
                DbConnection.Connect(dbcon);
                using (var Cmd = dbcon.CreateCommand()) {
                    Cmd.CommandText = string.Format("UPDATE clients SET auth_status=1, banned_reason=@banned_reason WHERE cpukey=@cpukey;");
                    Cmd.Parameters.AddWithValue("@cpukey", ClientObj.cpukey);
                    Cmd.Parameters.AddWithValue("@banned_reason", reason);
                    Cmd.ExecuteNonQuery();
                    dbcon.Close();
                }
            }
        }

        public static int Countconsolesusingcpu(CLIENT_STRUCT ClientObj) {
            int ConsolesFoundUsingCpu = 0;
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {

                    Cmd.CommandText = string.Format("SELECT COUNT(*) FROM clients WHERE cpukey=@cpukey");
                    Cmd.Parameters.AddWithValue("@cpukey", ClientObj.cpukey);
                    ConsolesFoundUsingCpu = Convert.ToInt32(Cmd.ExecuteScalar());
                    DbCon.Close();
                }
            }
            return ConsolesFoundUsingCpu;
        }
        
        public static int GetOnlineClients() {
            int Count = 0;
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {

                    Cmd.CommandText = string.Format("SELECT COUNT(*) FROM clients WHERE online=1");
                    Count = Convert.ToInt32(Cmd.ExecuteScalar());
                    DbCon.Close();
                }
            }
            return Count;
        }

        public static int GetClientsCount() {
            int Count = 0;
            using (var DbCon = DbConnection.SetupConnection()) {
                DbConnection.Connect(DbCon);
                using (var Cmd = DbCon.CreateCommand()) {

                    Cmd.CommandText = string.Format("SELECT COUNT(*) FROM clients");
                    Count = Convert.ToInt32(Cmd.ExecuteScalar());
                    DbCon.Close();
                }
            }
            return Count;
        }
    }
}