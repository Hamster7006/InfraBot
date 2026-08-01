using InfraBot.Entities;
using InfraBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.HelpData
{
    internal partial class ConstantData
    {
        internal static ReplyKeyboardMarkup CreateReplyKeyboardMarkup(BotUser? user)
        {
            var keyboard = new ReplyKeyboardMarkup { ResizeKeyboard = true };

            if (user == null)
            {
                keyboard.AddNewRow([Start]);
                return keyboard;
            }

            switch (user.Status)
            {
                case UserStatus.Guest:
                    keyboard.AddNewRow([Pending]);
                    break;

                case UserStatus.Operator:
                    keyboard.AddNewRow([ListServers]);
                    keyboard.AddNewRow([Pending]);
                    break;

                case UserStatus.MainOperator:
                    keyboard.AddNewRow([ListServers]);
                    break;

                case UserStatus.Admin:
                    keyboard.AddNewRow([ListServers, ListScripts, UserControl]);
                    keyboard.AddNewRow([PendingRequests]);
                    break;
            }

            return keyboard;
        }
    }
}
