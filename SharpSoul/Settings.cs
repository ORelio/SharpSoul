using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SharpSoul
{
    class Settings
    {
        public static string Login = "";
        public static string Password = "";
        public static string Location = "Somewhere";
        public static List<string> Friends = new List<string>();
        public static string ConsoleTitle = "%username% - " + Program.Name;
        public static bool BeepOnMessage = false;

        /// <summary>
        /// Load settings from the give INI file
        /// </summary>
        /// <param name="settingsfile">File to load</param>

        public static void LoadSettings(string settingsfile)
        {
            if (File.Exists(settingsfile))
            {
                try
                {
                    string[] Lines = File.ReadAllLines(settingsfile);
                    foreach (string lineRAW in Lines)
                    {
                        string line = lineRAW.Split('#')[0].Trim();
                        if (line.Length > 0)
                        {
                            string argName = line.Split('=')[0];
                            if (line.Length > (argName.Length + 1))
                            {
                                string argValue = line.Substring(argName.Length + 1);
                                switch (argName.ToLower())
                                {
                                    case "login": Login = argValue; break;
                                    case "password": Password = argValue; break;
                                    case "ns-server": NetsoulClient.NSServer = argValue; break;
                                    case "ns-port": try { NetsoulClient.NSServerPort = Int32.Parse(argValue); } catch { NetsoulClient.NSServerPort = 4242; } break;
                                    case "consoletitle": ConsoleTitle = argValue; break;
                                    case "friends": foreach (string friend in argValue.Split(',')) { Friends.Add(friend); } break;
                                    case "beeponmsg": BeepOnMessage = argValue.ToLower() == "true"; break;
                                    case "location": Location = argValue; break;
                                }
                            }
                        }
                    }
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Write an INI file with default settings
        /// </summary>
        /// <param name="settingsfile">File to (over)write</param>

        public static void WriteDefaultSettings(string settingsfile)
        {
            System.IO.File.WriteAllText(settingsfile, "#Netsoul Console Client v" + Program.Version + "\r\n"
                + "#Startup Config File\r\n"
                + "\r\n"
                + "#General settings\r\n"
                + "#leave blank to prompt user on startup.\r\n"
                + "\r\n"
                + "Login=" + Settings.Login + "\r\n"
                + "Password=" + Settings.Password + "\r\n"
                + "ConsoleTitle=" + Settings.ConsoleTitle + "\r\n"
                + "Location=" + Settings.Location + "\r\n"
                + "\r\n"
                + "#Friends list\r\n"
                + "#example: friends=login_x,login_y,login_z\r\n"
                + "\r\n"
                + "Friends=\r\n"
                + "BeepOnMsg=False\r\n"
                + "\r\n"
                + "#Network settings\r\n"
                + "#Do not change if you don't know what you are doing.\r\n"
                + "\r\n"
                + "NS-Server=" + NetsoulClient.NSServer + "\r\n"
                + "NS-Port=" + NetsoulClient.NSServerPort + "\r\n"
                , Encoding.UTF8);
        }
    }
}
