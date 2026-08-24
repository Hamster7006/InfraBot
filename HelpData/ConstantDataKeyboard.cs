using InfraBot.Entities;
using InfraBot.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.HelpData;

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
                keyboard.AddNewRow([Help, Info]);
                break;

            case UserStatus.Operator:
                keyboard.AddNewRow([ListServers]);
                keyboard.AddNewRow([Report]);
                keyboard.AddNewRow([Help, Info, Report]);
                break;

            case UserStatus.Admin:
                keyboard.AddNewRow([ListServers]);
                keyboard.AddNewRow([CreateColorControlButton(KeyboardButtonStyle.Danger, AdminControl)]);
                keyboard.AddNewRow([Help, Info, Report]);
                break;
        }

        return keyboard;
    }

    /// <summary>Клавиатура админ-модуля (после /admincontrol).</summary>
    internal static ReplyKeyboardMarkup CreateAdminModuleKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup { ResizeKeyboard = true };
        keyboard.AddNewRow([AddServer, ListServers]);
        keyboard.AddNewRow([AddScript, ListScripts]);
        keyboard.AddNewRow([AddSvcAccount, ListSvcAccounts]);
        keyboard.AddNewRow([PendingRequests, UserControl, ReportAll]);
        keyboard.AddNewRow([CreateColorControlButton(KeyboardButtonStyle.Danger, Cancel)]);
        return keyboard;
    }

    internal static ReplyKeyboardMarkup CreateCancelKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup { ResizeKeyboard = true };
        keyboard.AddNewRow([CreateColorControlButton(KeyboardButtonStyle.Danger, Cancel)]);
        return keyboard;
    }

    internal static KeyboardButton CreateColorControlButton(KeyboardButtonStyle keyboardButtonStyle, string command)
    {
        var button = new KeyboardButton(command);
        button.Style = keyboardButtonStyle;
        return button;
    }

}

