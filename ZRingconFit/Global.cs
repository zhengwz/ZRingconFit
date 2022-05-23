using BaseServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZRingconFit
{
    internal class Global
    {
        public static string YuzuUri = "";
        public static string UserUri = "";
        public static string GameUri = "";

        public static bool AutoStartGame = false;
        public static bool ReplaceConfig = false;

        public static void LoadConfig()
        {
            try
            {
                YuzuUri = ReadIniClass.getWithName("YuzuUri");
                UserUri = ReadIniClass.getWithName("UserUri");
                GameUri = ReadIniClass.getWithName("GameUri");

                string tempStr = ReadIniClass.getWithName("AutoStartGame");
                if (bool.TryParse(tempStr, out _))
                {
                    AutoStartGame = bool.Parse(tempStr);
                }

                tempStr = ReadIniClass.getWithName("ReplaceConfig");
                if (bool.TryParse(tempStr, out _))
                {
                    ReplaceConfig = bool.Parse(tempStr);
                }
            }
            catch { }

        }

        public static void SaveConfig()
        {
            try
            {
                ReadIniClass.setWithName("YuzuUri", YuzuUri);
                ReadIniClass.setWithName("UserUri", UserUri);
                ReadIniClass.setWithName("GameUri", GameUri);

                ReadIniClass.setWithName("AutoStartGame", AutoStartGame.ToString());
                ReadIniClass.setWithName("ReplaceConfig", ReplaceConfig.ToString());
            }
            catch { }
        }
    }
}
