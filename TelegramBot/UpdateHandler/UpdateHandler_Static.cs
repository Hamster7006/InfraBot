using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace InfraBot.TelegramBot
{
    internal partial class UpdateHandler
    {
        internal static Chat GetChatFromUpdate(Update update)
        {
            if (update.Message != null)
                return update.Message.Chat;

            if (update.CallbackQuery?.Message != null)
                return update.CallbackQuery.Message.Chat;

            if (update.EditedMessage != null)
                return update.EditedMessage.Chat;

            throw new InvalidOperationException("Не удалось определить чат из update");
        }

        internal static long GetChatIdFromUpdate(Update update)
            => GetChatFromUpdate(update).Id;

        internal static string? GetMessageFromUpdate(Update update)
        {
            if (update.Message?.Text != null)
                return update.Message.Text;

            if (update.CallbackQuery?.Message?.Text != null)
                return update.CallbackQuery.Message.Text;

            if (update.EditedMessage?.Text != null)
                return update.EditedMessage.Text;

            return null;
        }

        internal static long GetUserIdFromUpdate(Update update)
        {
            if (update.Message?.From != null)
                return update.Message.From.Id;

            if (update.CallbackQuery?.From != null)
                return update.CallbackQuery.From.Id;

            if (update.EditedMessage?.From != null)
                return update.EditedMessage.From.Id;

            throw new InvalidOperationException("Не удалось получить Id пользователя из update");
        }
    }
}
