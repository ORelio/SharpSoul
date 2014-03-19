using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpSoul
{
    class Program
    {
        public const string Name = "SharpSoul";
        public const string Version = "1.0";
        private static NetsoulClient client;

        /// <summary>
        /// Netsoul Console Client by ORelio (c) 2014.
        /// Allow to connect at school to the Netsoul network used by IONIS Education Group
        /// This source code is released under the CDDL 1.0 License.
        /// </summary>

        static void Main(string[] args)
        {
            Console.WriteLine(Program.Name + " - " + Translations.Get("program_description") + " - v" + Program.Version + " - " + Translations.Get("word_by") + " ORelio");

            //Process configuration file
            if (args.Length >= 1 && System.IO.File.Exists(args[0]) && System.IO.Path.GetExtension(args[0]).ToLower() == ".ini")
            {
                Settings.LoadSettings(args[0]);

                //remove ini configuration file from arguments array
                List<string> args_tmp = args.ToList<string>();
                args_tmp.RemoveAt(0);
                args = args_tmp.ToArray();
            }
            else if (System.IO.File.Exists(Program.Name + ".ini"))
            {
                Settings.LoadSettings(Program.Name + ".ini");
            }
            else Settings.WriteDefaultSettings(Program.Name + ".ini");

            //Process command-line arguments
            if (args.Length >= 1)
            {
                Settings.Login = args[0];
                if (args.Length >= 2)
                {
                    Settings.Password = args[1];
                }
            }

            //Ask User for Username and Password?
            while (Settings.Login == "")
            {
                Console.Write(Translations.Get("username") + " : ");
                Settings.Login = Console.ReadLine();
            }
            while (Settings.Password == "")
            {
                Console.Write(Translations.Get("password") + " : ");
                Settings.Password = ConsoleIO.ReadPassword();
                if (!ConsoleIO.basicIO)
                {
                    //Hide password length
                    string placeholder = Translations.Get("password") + " : <******>";
                    Console.CursorTop--; Console.Write(placeholder);
                    for (int i = placeholder.Length; i < Console.BufferWidth; i++) { Console.Write(' '); }
                }
            }

            //Console title
            if (Settings.ConsoleTitle != "")
            {
                Console.Title = Settings.ConsoleTitle.Replace("%username%", Translations.Get("window_new"));
            }

            //Launch the Netsoul Client
            ConsoleIO.SetAutoCompleteEngine(new FriendTabCompleter());
            InitializeClient();

            do //Main client loop for typing commands
            {
                string cmd = ConsoleIO.ReadLine().Trim();
                if (cmd != "")
                {
                    if (cmd[0] == '@')
                    {
                        string to = cmd.Split(' ')[0].Substring(1);
                        string msg = cmd.Substring(to.Length + 2);
                        client.SendMessage(to, "", msg);
                    }
                    else
                    {
                        cmd = cmd.ToLower();
                        string[] splitted = cmd.Split(' ');
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        if (cmd == "exit" || cmd == "quit" || cmd == "/quit")
                        {
                            client.Disconnect();
                            Environment.Exit(0);
                        }
                        else if (splitted[0] == "add")
                        {
                            if (splitted.Length > 1)
                            {
                                client.ContactAdd(splitted[1]);
                                Settings.Friends.Add(splitted[1]);
                                ConsoleIO.WriteLine(Translations.Get("command_added") + ' ' + splitted[1] + '.');
                            }
                            else ConsoleIO.WriteLine(Translations.Get("command_usage") + ": add frien_d");
                        }
                        else if (splitted[0] == "rm")
                        {
                            if (splitted.Length > 1)
                            {
                                client.ContactRemove(splitted[1]);
                                Settings.Friends.Remove(splitted[1]);
                                ConsoleIO.WriteLine(Translations.Get("command_removed") + ' ' + splitted[1] + '.');
                            }
                            else ConsoleIO.WriteLine(Translations.Get("command_usage") + ": rm frien_d");
                        }
                        else ConsoleIO.WriteLine(Translations.Get("command_help_intro")
                               + '\n' + "add: " + Translations.Get("command_help_add")
                               + '\n' + "rm: " + Translations.Get("command_help_remove")
                               + '\n' + "exit: " + Translations.Get("command_help_exit") + ' ' + Program.Name
                               + '\n' + "@login_x " + Translations.Get("command_help_message")
                               );
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            } while (true);
        }

        //Initialize the client
        private static void InitializeClient()
        {
            if (client != null) { client.Disconnect(); }
            client = new NetsoulClient(Settings.Login, Settings.Password, Settings.Location, Program.Name, Settings.Friends);
            client.PollResult += new NetsoulClient.PollResultEventHandler(onPollResult);
            client.StateChange += new NetsoulClient.StateChangeEventHandler(onStateChange);
            client.MessageReceived += new NetsoulClient.MessageReceivedEventHandler(onMessageReceived);
            client.LoginStateChange += new NetsoulClient.LoginStateChangeEventHandler(onLoginChange);
        }

        //Print status of friends
        private static void onPollResult(object sender, NetsoulPollResultEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            string state = "";
            switch (args.UserState)
            {
                case NetsoulPollResultEventArgs.StateInfo.Active: state = Translations.Get("word_active"); break;
                case NetsoulPollResultEventArgs.StateInfo.Away: state = Translations.Get("word_away"); break;
                case NetsoulPollResultEventArgs.StateInfo.Paladutout: state = Translations.Get("word_paladutout"); break;
                case NetsoulPollResultEventArgs.StateInfo.NoState: state = Translations.Get("word_connected"); break;
            }
            ConsoleIO.WriteLine(Translations.Get("prefix_poll_result") + args.UserLogin + ' ' + Translations.Get("word_is") + ' ' + state);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        //Print status updates of friends
        private static void onStateChange(object sender, NetsoulStateChangeEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            string state = "";
            switch (args.UserNewState)
            {
                case NetsoulStateChangeEventArgs.StateInfo.Active: state = Translations.Get("word_active"); break;
                case NetsoulStateChangeEventArgs.StateInfo.Away: state = Translations.Get("word_away"); break;
                case NetsoulStateChangeEventArgs.StateInfo.Paladutout: state = Translations.Get("word_paladutout"); break;
                case NetsoulStateChangeEventArgs.StateInfo.Login: state = Translations.Get("word_connected"); break;
                case NetsoulStateChangeEventArgs.StateInfo.Logout: state = Translations.Get("word_disconnected"); break;
            }
            ConsoleIO.WriteLine(Translations.Get("prefix_friend_status") + args.UserLogin + ' ' + Translations.Get("word_is") + ' ' + state);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        //Print messages from friends
        private static void onMessageReceived(object sender, NetsoulMessageReceivedEventArgs args)
        {
            ConsoleIO.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ' ' + args.SenderLogin + '@' + args.SenderLocation + ": " + args.Message.Replace("\\n", " "));
            if (Settings.BeepOnMessage) { Console.Beep(); }
        }

        //Print statuses of Netsoul client
        private static void onLoginChange(object sender, NetsoulLoginStateChangeEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            string status = "";

            switch (args.LoginState)
            {
                case NetsoulClient.LoginState.Connecting: status = Translations.Get("status_connecting"); break;
                case NetsoulClient.LoginState.ConnectedNotLogged: status = Translations.Get("status_not_logged_in"); break;
                case NetsoulClient.LoginState.LoggingIn: status = Translations.Get("status_logging_in"); break;
                case NetsoulClient.LoginState.LoggedIn: status = Translations.Get("status_logged_in"); break;
                case NetsoulClient.LoginState.LoginFailed: status = Translations.Get("status_login_failed"); break;
                case NetsoulClient.LoginState.ConnectionFailed: status = Translations.Get("status_connection_failed"); break;
                case NetsoulClient.LoginState.ConnectionLost: status = Translations.Get("status_connection_lost"); break;
            }

            ConsoleIO.WriteLine(Translations.Get("prefix_ns_status") + status);
           
            //Update window title
            if (Settings.ConsoleTitle != "")
            {
                if (args.LoginState == NetsoulClient.LoginState.LoggedIn)
                {
                    Console.Title = Settings.ConsoleTitle.Replace("%username%", Settings.Login);
                }
                else Console.Title = Settings.ConsoleTitle.Replace("%username%", status);
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            //Automatically reconnect on connection lost or connection failed
            if (args.LoginState == NetsoulClient.LoginState.ConnectionFailed
             || args.LoginState == NetsoulClient.LoginState.ConnectionLost)
                { System.Threading.Thread.Sleep(1000); InitializeClient(); }
        }
    }

    //Autocomplete friend names with TAB key
    public class FriendTabCompleter : IAutoComplete
    {
        string IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (BehindCursor.StartsWith("@"))
            {
                BehindCursor = BehindCursor.Substring(1).ToLower();
                foreach (string friend in Settings.Friends)
                {
                    if (friend.ToLower().StartsWith(BehindCursor))
                    {
                        return "@" + friend + ' ';
                    }
                }
            }
            return "";
        }
    }
}
