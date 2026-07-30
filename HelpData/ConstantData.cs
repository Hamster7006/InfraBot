using InfraBot.Entities;
using InfraBot.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.HelpData
{
    internal class ConstantData
    {
        internal const string Start = "/start";
        internal const string Help = "/help";
        internal const string Info = "/info";
        internal const string Report = "/report";
        internal const string Cansel = "/Cansel";
        internal static readonly Dictionary<string, CommndsData>
            CommandsDictionary = new()
            {
                ["start"] = new CommndsData
                {
                    Command = "/start",
                    Description = "Регистрация",
                    Levels = [UserStatus.Guest, UserStatus.Admin, UserStatus.Pending]
                },
                ["serverslist"] = new CommndsData
                {
                    Command = "/serverslist",
                    Description = "Список серверов",
                    Levels = [UserStatus.Operator, UserStatus.Admin]
                },
                ["addserver"] = new CommndsData
                {
                    Command = "/addserver",
                    Description = "Список серверов",
                    Levels = [UserStatus.Admin]
                },
                //[""] = new CommndsData
                //{
                //    Command = "/",
                //    Description = "",
                //    Level = UserStatus.Guest
                //},
            };
        internal static string PrintHelpData(BotUser user)
        {
            var temp = "";
            var tempCommandsDictionary = CommandsDictionary.Where(x => x.Value.Levels.Contains(user.Status)).ToDictionary();
            foreach ( var key in tempCommandsDictionary.Keys)
            {
                temp += $"{tempCommandsDictionary[key].Command} - {tempCommandsDictionary[key].Description}\r\n";
            }   
            return temp;
        }
    }
    internal class CommndsData
    {
        internal string Command { get; set; } = null!;
        internal string Description { get; set; } = null!;
        internal List<UserStatus> Levels { get; set; } = [];
    }
}
