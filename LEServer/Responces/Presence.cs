using Security;
using System;
using System.Text;
using Newtonsoft.Json;

namespace LE.Responces {
    class Presence {
        public static void ProcessPresence(ClientHandler.ioData io, ref CLIENT_STRUCT ClientObj) {
            string SessionToken = Utilities.BytesToString(io.reader.ReadBytes(0x10));
            string TitleID = io.reader.ReadUInt32().ToString("X");
            byte[] GamerTag = io.reader.ReadBytes(0x10);
            string ConsoleKvStatus = io.reader.ReadUInt32().ToString("X");

            bool ClientFound = MySql.GetClient(ref ClientObj, SessionToken);
            
            byte[] DiscordToken = new byte[0xC];
            byte[] PresBuffer = new byte[DiscordToken.Length + 0xC]; //0x8
            EndianWriter Data = new EndianIO(PresBuffer, EndianStyle.BigEndian).Writer;
            
            if (ClientFound) {
                ClientObj.titleid = Utilities.TitleID(TitleID);
                ClientObj.ip = io.ipaddr.Address.ToString().Split(new char[] { ':' })[0];
                ClientObj.lastloginTime = DateTime.Now;
                ClientObj.gamertag = Utilities.Validategamertag(GamerTag);
                
                Data.Write((int)PACKET_STATUS.SUCCESS);

                if (ClientObj.consoleaction != CLIENT_ACTION.DEFAULT && ClientObj.actioncompleted == CLIENT_ACTION_COMPLETED.AWAITING) {
                    if (ClientObj.consoleaction == CLIENT_ACTION.DEFAULT)
                        Data.Write((int)CLIENT_ACTION.DEFAULT);
                    else if (ClientObj.consoleaction == CLIENT_ACTION.REBOOT)
                        Data.Write((int)CLIENT_ACTION.REBOOT);
                    else if (ClientObj.consoleaction == CLIENT_ACTION.RROD)
                        Data.Write((int)CLIENT_ACTION.RROD);
                    else if (ClientObj.consoleaction == CLIENT_ACTION.SENDTODASH)
                        Data.Write((int)CLIENT_ACTION.SENDTODASH);
                } else {
                    Data.Write((int)CLIENT_ACTION.DEFAULT);
                }
            } else {
                Data.Write((int)PACKET_STATUS.ERROR);
                Data.Write((int)CLIENT_ACTION.DEFAULT);
            }

            int DiscordPopup = 0;
            
            if (ClientObj.discord != null) {
                Discord discord = JsonConvert.DeserializeObject<Discord>(ClientObj.discord);
                if (discord.id != "0" && discord.primary && !discord.verified && discord.token != null) {
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(discord.token), 0, DiscordToken, 0, discord.token.Length);
                    DiscordPopup = Convert.ToInt32(discord.popup);
                    discord.popup = false;

                    ClientObj.discord = JsonConvert.SerializeObject(discord);
                }
            }


            Utilities.Update_LiveStatus(ConsoleKvStatus, ref ClientObj);
            MySql.SaveClient(ClientObj, SessionToken);
            MySql.UpdateKvThread(ClientObj);

            Data.Write(DiscordToken);
            Data.Write(DiscordPopup);
            io.writer.Write(PresBuffer);
        }
    }
}