using Security;
using LE;
using System.Text;
using System;
using Newtonsoft.Json.Linq;

namespace LE.Responces {
    class Games {
        public static void ProcessPatches(ClientHandler.ioData io, ref CLIENT_STRUCT ClientObj) {
            string SessionToken = Utilities.BytesToString(io.reader.ReadBytes(0x10));
            uint TitleId = io.reader.ReadUInt32();

            byte[] Resp = new byte[0x8];
            EndianWriter Data = new EndianIO(Resp, EndianStyle.BigEndian).Writer;

            bool ClientFound = MySql.GetClient(ref ClientObj, SessionToken);
            if (ClientFound && ClientObj.authstatus >= CLIENT_AUTHSTATUS.AUTHED || LEServer.Freemode) {
                Data.Write((int)PACKET_STATUS.SUCCESS);

                TITLEIDS RunningGame = (TITLEIDS)TitleId;
                switch (RunningGame) {
                    case TITLEIDS.XUI:
                        Console.WriteLine("called or whateva");
                        // size of patches gonna send across network 0x8 2 dwords / resp with size info
                        Data.Write(0x8);
                        io.writer.Write(Resp);

                        // purpose is why waste an entire resp for one thing
                        byte[] XuiResp = new byte[0x8];
                        EndianWriter XuiRespData = new EndianIO(XuiResp, EndianStyle.BigEndian).Writer;
                        JObject UiColorsObj = JObject.Parse(ClientObj.ui_colors);
                        Console.WriteLine(BitConverter.ToUInt32(Utilities.StringToBytes("0xFF" + UiColorsObj["uicolorprimary"].ToString()), 0));

                        XuiRespData.Write(BitConverter.ToUInt32(Utilities.StringToBytes("0xFF" + UiColorsObj["uicolorprimary"].ToString()), 0));
                        XuiRespData.Write(BitConverter.ToUInt32(Utilities.StringToBytes("0xFF" + UiColorsObj["uicoloronpress"].ToString()), 0));
                        io.writer.Write(XuiResp);
                        break;

                    case TITLEIDS.BO2:
                        Data.Write(LEServer.ServerModuleObj.B02BypassData.Length);
                        io.writer.Write(Resp);
                        io.writer.Write(LEServer.ServerModuleObj.B02BypassData);
                        break;

                    case TITLEIDS.COD_GHOSTS:
                        Data.Write(LEServer.ServerModuleObj.GhostsBypassData.Length);
                        io.writer.Write(Resp);
                        io.writer.Write(LEServer.ServerModuleObj.GhostsBypassData);
                        break;

                    default:
                        Data.Write(0);
                        io.writer.Write(Resp);
                        io.writer.Write((int)PACKET_STATUS.ERROR);
                        break;
                }
            }
            else {
                Data.Write((int)PACKET_STATUS.ERROR);
                Data.Write(0);
                io.writer.Write(Resp);
                io.writer.Write((int)PACKET_STATUS.ERROR);
            }
            MySql.SaveClient(ClientObj, SessionToken);
        }
    }
}
