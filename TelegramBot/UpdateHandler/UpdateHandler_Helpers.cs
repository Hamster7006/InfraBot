using InfraBot.Entities;
using InfraBot.HelpData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot
{
    internal partial class UpdateHandler
    {
        private async Task SendErrorComand(Chat chat, string text, BotUser userData, ReplyKeyboardMarkup replyKeyboardMarkup, CancellationToken ct)
        {
            await _telegramBotClient.SendMessage(chat,
                ConstantData.ReplaceText(
                    $"Команда '{text}' не найдена. Доступные команды:\r\n {ConstantData.HelpData(userData)}",
                    userData),
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: ct);
        }

        internal async Task<bool> CheckAnonimus(BotUser? userData, Chat chat, ReplyKeyboardMarkup replyKeyboardMarkup, CancellationToken ct)
        {
            if (userData == null)
            {
                await _telegramBotClient.SendMessage(chat,
                    $"Для запуска бота введите '{ConstantData.Start}'",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: ct);
                return true;
            }
            return false;
        }
    }
}
