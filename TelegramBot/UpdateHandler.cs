using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace InfraBot.TelegramBot
{
    
    internal class UpdateHandler : IUpdateHandler
    {
        public UpdateHandler()
        { }


        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public async Task OnMessage(Update update, Message message, CancellationToken cancellationToken)
        {
        }
        public async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
        }
        public async Task OnUnknown(Update update)
        {
            throw new NotImplementedException();
        }

        internal static Chat GetChatFromUpdate(Update update)
        {
            if (update.Message != null)
                return update.Message.Chat;

            if (update.CallbackQuery != null)
                return update.CallbackQuery.Message.Chat;

            if (update.EditedMessage != null)
                return update.EditedMessage.Chat;

            throw new InvalidOperationException("Не удалось определить чат из update");
        }
        internal static long GetChatIdFromUpdate(Update update)
        {
            return GetChatFromUpdate(update).Id;
        }
        internal static string GetMessageFromUpdate(Update update)
        {
            if (update.Message != null)
                return update.Message.Text;

            if (update.CallbackQuery != null)
                return update.CallbackQuery.Message.Text;

            if (update.EditedMessage != null)
                return update.EditedMessage.Text;

            throw new InvalidOperationException("Не удалось получить сообщение из update");
        }
        internal static long GetUserIdFromUpdate(Update update)
        {
            if (update.Message != null)
                return update.Message.From.Id;

            if (update.CallbackQuery != null)
                return update.CallbackQuery.From.Id;

            if (update.EditedMessage != null)
                return update.EditedMessage.From.Id;

            throw new InvalidOperationException("Не удалось получить Id пользователя из update");
        }
    }
}
