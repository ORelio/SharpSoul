using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpSoul
{
    /// <summary>
    /// Allow to get the whole app in English or French
    /// </summary>

    public static class Translations
    {
        private static Dictionary<string, string> translations;

        /// <summary>
        /// Return a tranlation for the requested text
        /// </summary>
        /// <param name="msg_name">text identifier</param>
        /// <returns>returns translation for this identifier</returns>

        public static string Get(string msg_name)
        {
            if (translations == null)
                init_messages();

            if (translations.ContainsKey(msg_name))
                return translations[msg_name];

            return msg_name.ToUpper();
        }

        /// <summary>
        /// Initialize translations to French or English depending on system language
        /// </summary>

        private static void init_messages()
        {
            translations = new Dictionary<string, string>();
            
            if (System.Globalization.CultureInfo.CurrentCulture.ThreeLetterISOLanguageName == "fra")
            {
                translations["word_by"] = "Par";
                translations["word_is"] = "est";
                translations["word_active"] = "actif";
                translations["word_away"] = "absent";
                translations["word_paladutout"] = "pas là du tout";
                translations["word_connected"] = "connecté";
                translations["word_disconnected"] = "déconnecté";
                translations["prefix_ns_status"] = "Statut: ";
                translations["prefix_poll_result"] = "Sondage: ";
                translations["prefix_friend_status"] = "Statut: ";
                translations["program_description"] = "Un client Netsoul en console";
                translations["username"] = "Login";
                translations["password"] = "Mot de passe";
                translations["window_new"] = "Nouvelle fenêtre";
                translations["command_usage"] = "Mode d'emploi";
                translations["command_added"] = "Ajouté";
                translations["command_removed"] = "Enlevé";
                translations["command_help_intro"] = "Commande non reconnue. Commandes disponible :";
                translations["command_help_add"] = "ajouter un ami pour la durée de la session";
                translations["command_help_remove"] = "retirer un ami pour la durée de la session";
                translations["command_help_exit"] = "se déconnecter et quitter";
                translations["command_help_message"] = "ceci est un message";
                translations["status_connecting"] = "Connexion en cours";
                translations["status_not_logged_in"] = "Connecté au serveur";
                translations["status_logging_in"] = "Identification";
                translations["status_logged_in"] = "Identifié";
                translations["status_login_failed"] = "Identification échouée";
                translations["status_connection_failed"] = "Connexion échouée";
                translations["status_connection_lost"] = "Connexion perdue";
            }
            else
            {
                translations["word_by"] = "By";
                translations["word_is"] = "is";
                translations["word_active"] = "active";
                translations["word_away"] = "away";
                translations["word_paladutout"] = "not here at all";
                translations["word_connected"] = "online";
                translations["word_disconnected"] = "offline";
                translations["prefix_ns_status"] = "NS-Status: ";
                translations["prefix_poll_result"] = "Poll Result: ";
                translations["prefix_friend_status"] = "Friend Status: ";
                translations["program_description"] = "A Netsoul Console Client";
                translations["username"] = "Login";
                translations["password"] = "Password";
                translations["window_new"] = "New Window";
                translations["command_usage"] = "Usage";
                translations["command_added"] = "Added";
                translations["command_removed"] = "Removed";
                translations["command_help_intro"] = "Unknown command. Available commands:";
                translations["command_help_add"] = "add a friend during this session";
                translations["command_help_remove"] = "remove a friend during this session";
                translations["command_help_exit"] = "disconnect and close";
                translations["command_help_message"] = "this is a message";
                translations["status_connecting"] = "Connecting";
                translations["status_not_logged_in"] = "Not logged in";
                translations["status_logging_in"] = "Logging in";
                translations["status_logged_in"] = "Logged in";
                translations["status_login_failed"] = "Login failed";
                translations["status_connection_failed"] = "Connection failed";
                translations["status_connection_lost"] = "Connection lost";
            }
        }
    }
}
