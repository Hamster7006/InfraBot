using InfraBot.Entities;
using InfraBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.HelpData
{
    internal partial class ConstantData
    {
        //internal static bool IsCommandAllowed(string key, UserStatus status)
        //=> CommandsDictionary.TryGetValue(key, out var cmd) && cmd.Levels.Contains(status);

        internal static string HelpData(BotUser user)
        {
            var temp = "";
            var available = CommandsDictionary
                .Where(x => x.Value.Levels.Contains(user.Status))
                .ToDictionary();

            foreach (var key in available.Keys)
                temp += $"{key} - {available[key].Description}\r\n";

            return temp;
        }

        static internal string ReplaceText(string text, BotUser? userName)
        {
            if (userName != null)
                text = $"{userName.Username},\r\n{text}";
            return text;
        }
    }
}
