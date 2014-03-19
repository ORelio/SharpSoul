using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SharpSoul
{
    /// <summary>
    /// SharpSoul Netsoul Client v1.0 - By ORelio
    /// Can be used in another project as a standalone class
    /// </summary>

    public class NetsoulClient
    {
        public static string NSServer = "ns-server.epita.fr";
        public static int NSServerPort = 4242;
        public const string Version = "1.0";

        public enum LoginState { Connecting, ConnectedNotLogged, LoggingIn, LoggedIn, LoginFailed, ConnectionFailed, ConnectionLost };

        private string username;
        private string password;
        private string location;
        private string client_name;
        private Thread t_updater;
        private Thread t_pinger;
        private TcpClient client;
        public static int login_attempts_left = 0;
        private LoginState state = LoginState.Connecting;
        private List<string> contacts = new List<string>();

        /// <summary>
        /// Create a new Netsoul client and starts the login procedure
        /// </summary>
        /// <param name="username">Username (login_x)</param>
        /// <param name="password">SOCKS password (50cKSP4s5)</param>
        /// <param name="location">Location of connection</param>
        /// <param name="client_name">Client name</param>
        /// <param name="contacts">Contact list (friend_1, friend_2...)</param>

        public NetsoulClient(string username, string password, string location, string client_name, IEnumerable<string> contacts)
            :this(username, password, location, client_name)
        {
            foreach (string login in contacts)
            {
                if (isValidLogin(login) && !this.contacts.Contains(login))
                {
                    this.contacts.Add(login);
                }
            }
        }

        /// <summary>
        /// Create a new Netsoul client and starts the login procedure
        /// </summary>
        /// <param name="username">Username (login_x)</param>
        /// <param name="password">SOCKS password (50cKSP4s5)</param>
        /// <param name="location">Location of connection</param>
        /// <param name="client_name">Client name</param>

        public NetsoulClient(string username, string password, string location, string client_name)
            :this(username, password)
        {
            this.location = location;
            this.client_name = client_name;
        }

        /// <summary>
        /// Create a new Netsoul client and starts the login procedure
        /// </summary>
        /// <param name="username">Username (login_x)</param>
        /// <param name="password">SOCKS password (50cKSP4sS)</param>

        public NetsoulClient(string username, string password)
        {
            this.username = username;
            this.password = password;

            //Netsoul Daemon Thread
            t_updater = new Thread(new ThreadStart(Updater));
            t_updater.Name = "NetsoulDaemon";
            t_updater.Start();
        }

        /// <summary>
        /// Debug raw command sending console prompt.
        /// </summary>

        private void DebugCMDSender()
        {
            try
            {
                while (client.Client.Connected)
                {
                    string text = Console.ReadLine() + '\n';
                    byte[] buffer = Encoding.ASCII.GetBytes(text);
                    this.client.Client.Send(buffer);
                }
            }
            catch (IOException) { }
        }

        /// <summary>
        /// Thread for sending keep-alive pings every minute
        /// </summary>

        private void Pinger()
        {
            try
            {
                while (client.Client.Connected)
                {
                    this.client.Client.Send(Encoding.ASCII.GetBytes("ping"));
                    Thread.Sleep(60000);
                }
            }
            catch (IOException) { }
            catch (SocketException) { LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectionLost)); }
        }

        /// <summary>
        /// Main network reading loop
        /// </summary>

        private void Updater()
        {
            int salut_socket = 0;
            string salut_hash = "";
            string salut_host = "";
            int salut_port = 0;
            long salut_timestamp = 0;

            if (this.location == "")
                this.location = "Somewhere";
            if (this.client_name == "")
                this.client_name = "SharpSoulLib-v" + Version;

            //Connect to the Netsoul server
            try
            {
                Thread.Sleep(50); //Prevent NullReferenceException when triggering the first event
                LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.Connecting));
                client = new TcpClient(NSServer, NSServerPort); client.ReceiveBufferSize = 1024 * 1024;
                LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectedNotLogged));
                
                //Debug command sending thread, allowing user input
                /*Thread t_sender = new Thread(new ThreadStart(DebugCMDSender));
                t_sender.Name = "NetsoulDebugCommandSender";
                t_sender.Start();*/
            }
            catch (SocketException)
            {
                LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectionFailed));
                return;
            }

            try
            {
                do
                {
                    //Read a line from the Socket
                    char c;
                    byte[] buffer = new byte[1];
                    StringBuilder str = new StringBuilder();
                    do
                    {
                        this.client.Client.Receive(buffer, 1, SocketFlags.None);
                        c = (char)buffer[0];
                        str.Append(c);
                    } while (c != '\n');
                    string text = str.ToString();

                    //Debug
                    //ConsoleIO.Write(text);

                    //Process received command
                    string[] splitted = text.Split(' ');
                    switch (splitted[0])
                    {
                        //Handshake request
                        case "salut":
                            if (splitted.Length >= 6)
                            {
                                salut_socket = Convert.ToInt32(splitted[1]);
                                salut_hash = splitted[2];
                                salut_host = splitted[3];
                                salut_port = Convert.ToInt32(splitted[4]);
                                salut_timestamp = Convert.ToInt64(splitted[5]);
                                if (state == LoginState.ConnectedNotLogged)
                                    sendCommand("auth_ag ext_user none none");
                            }
                            break;

                        //Command result code
                        case "rep":
                            switch (state)
                            {
                                case LoginState.ConnectedNotLogged:
                                    if (splitted[1] == "002")
                                    {
                                        //Handshake received
                                        MD5CryptoServiceProvider crypto = new MD5CryptoServiceProvider();
                                        byte[] tohash = Encoding.ASCII.GetBytes(salut_hash + '-' + salut_host + '/' + salut_port + password);
                                        byte[] hashed = crypto.ComputeHash(tohash);
                                        StringBuilder result = new StringBuilder();
                                        foreach (byte b in hashed)
                                            result.Append(b.ToString("x2").ToLower());
                                        sendCommand("ext_user_log " + username + ' ' + result.ToString() + ' ' + myURLencode(location) + ' ' + myURLencode(client_name));
                                        LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.LoggingIn));
                                    }
                                    break;

                                case LoginState.LoggingIn:
                                    switch (splitted[1])
                                    {
                                        case "002":
                                            //Login request accepted
                                            LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.LoggedIn));
                                            sendCommand("user_cmd msg *:www@*sharpsoul-daemon* msg version+" + Version); //Identify the client
                                            sendCommand("user_cmd who {" + string.Join(",", contacts) + "}"); //Poll request
                                            sendCommand("user_cmd watch_log_user {" + string.Join(",", contacts) + "}"); //Watch request
                                            //Keeep-alive pinger thread
                                            t_pinger = new Thread(new ThreadStart(Pinger));
                                            t_pinger.Name = "NetsoulDaemon_Pinger";
                                            t_pinger.Start();
                                            break;

                                        case "033":
                                            //Login request refused
                                            LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.LoginFailed));
                                            break;
                                    }
                                    break;
                            }
                            break;

                        case "ping":
                            //Keep-Alive
                            sendCommand("ping");
                            break;

                        case "user_cmd":
                            //Message related to another user
                            string[] user_cmd_temp = text.Split('|');
                            if (user_cmd_temp.Length == 2)
                            {
                                string[] user_cmd = user_cmd_temp[0].Split(' ');
                                string[] user_cmd_args = user_cmd_temp[1].Split(' ');
                                if (user_cmd_args[0] == "")
                                {
                                    List<string> tmp = user_cmd_args.ToList<string>();
                                    tmp.RemoveAt(0);
                                    user_cmd_args = tmp.ToArray();
                                }
                                switch (user_cmd_args[0])
                                {
                                    case "who":
                                        if (user_cmd_args.Length > 1 && user_cmd_args[1] != "rep")
                                        {
                                            try
                                            {
                                                //Result of a poll request, giving data about a user
                                                int UserID = user_cmd_args.Length >= 2 ? Int32.Parse(user_cmd_args[1]) : 0;
                                                string UserLogin = user_cmd_args.Length >= 3 ? user_cmd_args[2] : "";
                                                string UserIP = user_cmd_args.Length >= 4 ? user_cmd_args[3] : "";
                                                long TimestampConnection = user_cmd_args.Length >= 5 ? Int64.Parse(user_cmd_args[4]) : 0;
                                                long TimestampLastActivity = user_cmd_args.Length >= 6 ? Int64.Parse(user_cmd_args[5]) : 0;
                                                string UserLocation = Uri.UnescapeDataString(user_cmd_args.Length >= 10 ? user_cmd_args[9] : "");
                                                string UserGroup = user_cmd_args.Length >= 11 ? user_cmd_args[10] : "";
                                                string UserStateRAW = user_cmd_args.Length >= 12 ? user_cmd_args[11] : ""; UserStateRAW += ":0";
                                                string[] UserStateTemp = UserStateRAW.Split(':');
                                                NetsoulPollResultEventArgs.StateInfo UserState = NetsoulPollResultEventArgs.StateInfo.NoState;
                                                switch (UserStateTemp[0].ToLower())
                                                {
                                                    case "connection": UserState = NetsoulPollResultEventArgs.StateInfo.NoState; break;
                                                    case "actif": UserState = NetsoulPollResultEventArgs.StateInfo.Active; break;
                                                    case "away": UserState = NetsoulPollResultEventArgs.StateInfo.Away; break;
                                                    case "paladutout": UserState = NetsoulPollResultEventArgs.StateInfo.Paladutout; break;
                                                }
                                                long TimestampUserState = Int64.Parse(UserStateTemp[1]);
                                                string UserData = Uri.UnescapeDataString(user_cmd_args.Length >= 13 ? user_cmd_args[12] : "");
                                                NetsoulPollResultEventArgs arguments = new NetsoulPollResultEventArgs(UserID, UserLogin, UserIP,
                                                    TimestampConnection, TimestampLastActivity, UserLocation, UserGroup, UserData, UserState, TimestampUserState);
                                                PollResult(this, arguments);
                                            }
                                            catch (IndexOutOfRangeException) { /* Invalid message: missing arguments */ }
                                            catch (OverflowException) { /* Invalid message: invalid timestamps */ }
                                            catch (FormatException) { /* Invalid message: invalid timestamps */ }
                                        }
                                        break;

                                    case "login":
                                    case "logout":
                                    case "state":
                                    case "msg":
                                        try
                                        {
                                            //User state change notification or user message
                                            string[] user_cmd_data = user_cmd[1].Split(':');
                                            int UserID = Int32.Parse(user_cmd_data[0]);
                                            string[] login_ip = user_cmd_data[3].Split('@');
                                            string UserLogin = login_ip[0];
                                            string UserIP = login_ip[1];
                                            string UserLocation = Uri.UnescapeDataString(user_cmd_data[5]);
                                            string UserGroup = user_cmd_data[6];
                                            if (user_cmd_args[0] == "msg")
                                            {
                                                //User message
                                                string Message = Uri.UnescapeDataString(user_cmd_args[1]);
                                                string[] temp_dst = user_cmd_args[2].Split(':')[1].Split('@');
                                                string MessageLoginDest = temp_dst[0];
                                                string MessageLocationDest = temp_dst[1].Trim('*');
                                                NetsoulMessageReceivedEventArgs arguments = new NetsoulMessageReceivedEventArgs( UserID, UserLogin,
                                                    UserIP, UserLocation, UserGroup, MessageLoginDest, MessageLocationDest, Message);
                                                MessageReceived(this, arguments);
                                            }
                                            else
                                            {
                                                //User state change notification
                                                NetsoulStateChangeEventArgs.StateInfo UserState = NetsoulStateChangeEventArgs.StateInfo.Login;
                                                long TimestampUserState = 0;
                                                switch (user_cmd_args[0].ToLower())
                                                {
                                                    case "login": UserState = NetsoulStateChangeEventArgs.StateInfo.Login; break;
                                                    case "logout": UserState = NetsoulStateChangeEventArgs.StateInfo.Logout; break;
                                                    case "state":
                                                        string[] UserStateTemp = user_cmd_args[1].Split(':');
                                                        switch (UserStateTemp[0].ToLower())
                                                        {
                                                            case "actif": UserState = NetsoulStateChangeEventArgs.StateInfo.Active; break;
                                                            case "away": UserState = NetsoulStateChangeEventArgs.StateInfo.Away; break;
                                                            case "paladutout": UserState = NetsoulStateChangeEventArgs.StateInfo.Paladutout; break;
                                                        }
                                                        TimestampUserState = Int64.Parse(UserStateTemp[1]);
                                                        break;
                                                }
                                                NetsoulStateChangeEventArgs arguments = new NetsoulStateChangeEventArgs(UserID, UserLogin, UserIP, UserLocation, UserGroup, UserState, TimestampUserState);
                                                StateChange(this, arguments);
                                            }
                                        }
                                        catch (IndexOutOfRangeException) { /* Invalid message: missing arguments */ }
                                        catch (OverflowException) { /* Invalid message: invalid timestamps */ }
                                        catch (FormatException) { /* Invalid message: invalid timestamps */ }   
                                        break;
                                }
                            }
                            break;
                    }
                } while (true);
            }
            catch (IOException) { LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectionLost)); }
            catch (SocketException) { LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectionLost)); }
            catch (ObjectDisposedException) { LoginStateChange(this, new NetsoulLoginStateChangeEventArgs(state = LoginState.ConnectionLost)); }
        }

        /// <summary>
        /// Private method. Allow the inner client to send raw netsoul commands
        /// </summary>
        /// <param name="text">raw command</param>

        private void sendCommand(string text)
        {

            //Debug
            /*Console.ForegroundColor = ConsoleColor.DarkGray;
            ConsoleIO.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;*/

            text += '\n';
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            this.client.Client.Send(buffer);
        }

        /// <summary>
        /// Send a message to someone over the Netsoul network
        /// </summary>
        /// <param name="toLogin">Recipient login_x</param>
        /// <param name="toLocation">Recipient location, set to "" to send to all locations for this login</param>
        /// <param name="Message">Message to send</param>
        /// <returns>Returns true if the message has been successfully sent</returns>

        public bool SendMessage(string toLogin, string toLocation, string Message)
        {
            if (!isValidLogin(toLogin))
                return false;
            if (this.state == LoginState.LoggedIn)
            {
                try
                {
                    string msg_enc = myURLencode(Message);
                    sendCommand("user_cmd msg *:" + Uri.EscapeDataString(toLogin) + "@*" + Uri.EscapeDataString(toLocation) + "* msg " + msg_enc);
                    return true;
                }
                catch { return false; }
            }
            else return false;
        }

        /// <summary>
        /// Proper URL encode method suitable for Netsoul protocol
        /// </summary>
        /// <param name="Message">Message to encode</param>
        /// <returns>Returs encoded string</returns>

        private static string myURLencode(string Message)
        {
            StringBuilder msg_enc = new StringBuilder();
            foreach (char c in Encoding.GetEncoding(1252).GetBytes(Message.Trim()))
            {
                if (c == '%')
                    msg_enc.Append("%25");
                else if (c == ' ')
                    msg_enc.Append("%20");
                else if ((c >= 'a' && c <= 'z')
                      || (c >= 'A' && c <= 'Z')
                      || (c >= '0' && c <= '9'))
                    msg_enc.Append(c);
                else
                    msg_enc.Append("%" + ((byte)c).ToString("x2"));
            }
            return msg_enc.ToString();
        }

        /// <summary>
        /// Verify that a login is valid
        /// </summary>
        /// <param name="login">Login to check (login_x)</param>
        /// <returns>True if the login is a valid login</returns>

        private static bool isValidLogin(string login)
        {
            Regex rgx = new Regex("[^a-z0-9_]");
            return rgx.Replace(login, "") == login;
        }

        /// <summary>
        /// Add a contact to the contact list and suscribe to status updates from this contact
        /// </summary>
        /// <param name="login">Login to add</param>
        /// <returns>True if the login is valid and the contact does not already exists</returns>

        public bool ContactAdd(string login)
        {
            if (isValidLogin(login) && !contacts.Contains(login))
            {
                contacts.Add(login);
                if (state == LoginState.LoggedIn)
                {
                    sendCommand("user_cmd who {" + login + "}"); //Poll request for the new contact
                    sendCommand("user_cmd watch_log_user {" + string.Join(",", contacts) + "}"); //Update watch list
                }
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Remove a contact from the contact list and unsuscribe to status updates from this contact
        /// </summary>
        /// <param name="login">Login to remove</param>
        /// <returns>True if the contact was removed properly, False if the contact did not exist</returns>

        public bool ContactRemove(string login)
        {
            if (contacts.Contains(login))
            {
                contacts.Remove(login);
                if (state == LoginState.LoggedIn)
                {
                    sendCommand("user_cmd watch_log_user {" + string.Join(",", contacts) + "}"); //Update watch list
                }
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Disconnect this client from the Netsoul server
        /// </summary>

        public void Disconnect()
        {
            try { sendCommand("exit"); } catch { }
            try { t_updater.Abort(); } catch { }
            try { t_pinger.Abort(); } catch { }
            try { client.Close(); } catch { }
        }

        /// <summary>
        /// Triggered when data about a contact is received.
        /// </summary>

        public event PollResultEventHandler PollResult;
        public delegate void PollResultEventHandler(object sender, NetsoulPollResultEventArgs e);

        /// <summary>
        /// Triggered when a contact logs in or changes his state
        /// </summary>

        public event StateChangeEventHandler StateChange;
        public delegate void StateChangeEventHandler(object sender, NetsoulStateChangeEventArgs e);

        /// <summary>
        /// Triggered when someone sends you a message
        /// </summary>

        public event MessageReceivedEventHandler MessageReceived;
        public delegate void MessageReceivedEventHandler(object sender, NetsoulMessageReceivedEventArgs e);

        /// <summary>
        /// Triggered during the Netsoul login process
        /// </summary>

        public event LoginStateChangeEventHandler LoginStateChange;
        public delegate void LoginStateChangeEventHandler(object sender, NetsoulLoginStateChangeEventArgs e);
    }

    /// <summary>
    /// A class containing data about a Poll Result Event, giving data about a contact
    /// </summary>

    public class NetsoulPollResultEventArgs : EventArgs
    {
        public enum StateInfo { NoState, Active, Away, Paladutout };

        private int user_id_;
        private string user_login_;
        private string user_ip_;
        private long timestamp_connection_;
        private long timestamp_lastactivity_;
        private string user_location_;
        private string user_group_;
        private string user_data_;
        private StateInfo user_state_;
        private long timestamp_user_state_;

        public NetsoulPollResultEventArgs(int user_id, string user_login, string user_ip, long timestamp_connection, long timestamp_last_activity,
            string user_location, string user_group, string user_data, StateInfo user_state, long timestamp_user_state)
        {
            this.user_id_ = user_id;
            this.user_login_ = user_login;
            this.user_ip_ = user_ip;
            this.timestamp_connection_ = timestamp_connection;
            this.timestamp_lastactivity_ = timestamp_last_activity;
            this.user_location_ = user_location;
            this.user_group_ = user_group;
            this.user_data_ = user_data;
            this.user_state_ = user_state;
            this.timestamp_user_state_ = timestamp_user_state;
        }

        public int UserID { get { return user_id_; } }
        public string UserLogin { get { return user_login_; } }
        public string UserIP { get { return user_ip_; } }
        public long TimestampConnection { get { return timestamp_connection_; } }
        public long TimestampLastActivity { get { return timestamp_lastactivity_; } }
        public string UserLocation { get { return user_location_; } }
        public string UserGroup { get { return user_group_; } }
        public string UserData { get { return user_data_; } }
        public StateInfo UserState { get { return user_state_; } }
        public long TimestampUserState { get { return timestamp_user_state_; } }
    }

    /// <summary>
    /// A class containing data about a State Change Event, giving the new status of a contact
    /// </summary>

    public class NetsoulStateChangeEventArgs : EventArgs
    {
        public enum StateInfo { Login, Active, Away, Paladutout, Logout };

        private int user_id_;
        private string user_login_;
        private string user_ip_;
        private string user_location_;
        private string user_group_;
        private StateInfo user_new_state_;
        private long timestamp_;

        public NetsoulStateChangeEventArgs(int user_id, string user_login, string user_ip, string user_location, string user_group, StateInfo newstate, long timestamp)
        {
            this.user_id_ = user_id;
            this.user_login_ = user_login;
            this.user_ip_ = user_ip;
            this.user_location_ = user_location;
            this.user_group_ = user_group;
            this.user_new_state_ = newstate;
            this.timestamp_ = timestamp;
        }

        public int UserID { get { return user_id_; } }
        public string UserLogin { get { return user_login_; } }
        public string UserIP { get { return user_ip_; } }
        public string UserLocation { get { return user_location_; } }
        public string UserGroup { get { return user_group_; } }
        public StateInfo UserNewState { get { return user_new_state_; } }
        public long TimeStamp { get { return timestamp_; } }
    }

    /// <summary>
    /// A class containing data about a message received from another user
    /// </summary>

    public class NetsoulMessageReceivedEventArgs : EventArgs
    {
        private int user_id_;
        private string user_login_;
        private string user_ip_;
        private string user_location_;
        private string user_group_;
        private string user_dest_login_;
        private string user_dest_location_;
        private string user_dest_message_;

        public NetsoulMessageReceivedEventArgs(int user_id, string user_login, string user_ip, string user_location, string user_group, string dest_user, string dest_location, string dest_message)
        {
            this.user_id_ = user_id;
            this.user_login_ = user_login;
            this.user_ip_ = user_ip;
            this.user_location_ = user_location;
            this.user_group_ = user_group;
            this.user_dest_login_ = dest_user;
            this.user_dest_location_ = dest_location;
            this.user_dest_message_ = dest_message;
        }

        public int UserID { get { return user_id_; } }
        public string SenderLogin { get { return user_login_; } }
        public string SenderIP { get { return user_ip_; } }
        public string SenderLocation { get { return user_location_; } }
        public string SenderGroup { get { return user_group_; } }
        public string RecipientLogin { get { return user_dest_login_; } }
        public string RecipientLocation { get { return user_dest_location_; } }
        public string Message { get { return user_dest_message_; } }
    }

    /// <summary>
    /// A class containing the new status of the Netsoul client during the login process
    /// </summary>

    public class NetsoulLoginStateChangeEventArgs : EventArgs
    {
        private NetsoulClient.LoginState loginstate_;

        public NetsoulLoginStateChangeEventArgs(NetsoulClient.LoginState newstate)
        {
            loginstate_ = newstate;
        }

        public NetsoulClient.LoginState LoginState { get { return loginstate_; } }
    }
}
