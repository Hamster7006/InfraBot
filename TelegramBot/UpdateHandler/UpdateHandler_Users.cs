using InfraBot.Core.Exceptions;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Callback;
using InfraBot.Scenarios.Tasks.User;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot;

/// <summary>
/// UI управления пользователями: списки, карточка, смена роли.
/// Запросы на повышение ставятся в очередь (/pending)),
/// обработка — через смену роли администратором.
/// </summary>
internal partial class UpdateHandler
{
    private int _currentUsersPage;

    /// <summary>Список пользователей или заявок на повышение (/usercontrol, /pendingrequests).</summary>
    private async Task ShowUsersListAsync(
        Update update,
        CallbackQuery? callbackQuery,
        bool pendingOnly,
        CancellationToken ct)
    {
        if (_userData == null)
            return;

        var chat = callbackQuery?.Message?.Chat ?? GetChatFromUpdate(update);
        var users = pendingOnly
            ? await _botUsersService.GetPendingElevationRequestsAsync(ct)
            : await _botUsersService.GetAllUsersAsync(ct);
        var userButtons = BuildUserButtonList(users);

        var listAction = pendingOnly ? "listpendingusers" : "listusers";
        var emptyText = pendingOnly
            ? "Нет активных заявок на повышение"
            : "Пользователи не найдены";
        var title = pendingOnly ? "Заявки на повышение" : "Пользователи";

        if (userButtons.Count == 0)
        {
            await ReplaceOrSendMessage(
                ConstantData.ReplaceText(emptyText, _userData),
                callbackQuery?.Message,
                chat,
                _replyKeyboardMarkup,
                ct);
            return;
        }

        if (callbackQuery?.Data != null)
        {
            try
            {
                var pagedCallback = PagedListCallbackDtoUsers.FromString(callbackQuery.Data);
                _currentUsersPage = pagedCallback.Page;
            }
            catch
            {
                _currentUsersPage = 0;
            }
        }

        var pageListDto = new PagedListCallbackDtoUsers
        {
            Action = listAction,
            ObjectID = null,
            Page = _currentUsersPage
        };

        var (inlineKeyboard, currentPage, totalPages) = BuildPagedButtons(userButtons, pageListDto);
        _currentUsersPage = currentPage;

        await ReplaceOrSendMessage(
            ConstantData.ReplaceText($"{title}\r\nСтраница {currentPage + 1} из {totalPages}", _userData),
            callbackQuery?.Message,
            chat,
            inlineKeyboard,
            ct);
    }

    /// <summary>Карточка пользователя: просмотр и смена роли.</summary>
    private async Task ShowUserDetailAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (_userData == null || callbackQuery.Message == null)
            return;

        var userDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (userDto.ObjectID == null)
            return;

        var user = await _botUsersService.GetUserByIdAsync(userDto.ObjectID.Value, ct);
        if (user == null)
        {
            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText("Пользователь не найден", _userData),
                cancellationToken: ct);
            return;
        }

        var detailText = BuildUserDetailText(user);
        var inlineKeyboard = BuildUserDetailKeyboard(user);

        await _telegramBotClient.EditMessageText(
            callbackQuery.Message.Chat,
            callbackQuery.Message.MessageId,
            ConstantData.ReplaceText(detailText, _userData),
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    /// <summary>Inline-выбор новой роли (все статусы кроме текущего).</summary>
    private async Task ShowUserStatusPickerAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (_userData == null || callbackQuery.Message == null)
            return;

        var userDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (userDto.ObjectID == null)
            return;

        var user = await _botUsersService.GetUserByIdAsync(userDto.ObjectID.Value, ct);
        if (user == null)
            return;

        var inlineKeyboard = new InlineKeyboardMarkup();
        foreach (UserStatus status in Enum.GetValues(typeof(UserStatus)))
        {
            if (status == user.Status)
                continue;

            var callback = $"setuserstatus|{user.Id}|{(int)status}";
            inlineKeyboard.AddNewRow(
                InlineKeyboardButton.WithCallbackData(UserControlScenario.FormatUserStatus(status), callback));
        }

        inlineKeyboard.AddNewRow(
            InlineKeyboardButton.WithCallbackData(
                "⬅️ Назад",
                $"showuserdetail|{user.Id}"));

        await _telegramBotClient.EditMessageText(
            callbackQuery.Message.Chat,
            callbackQuery.Message.MessageId,
            ConstantData.ReplaceText(
                $"Изменение роли: {UserControlScenario.FormatUserLabel(user)}\r\n" +
                $"Текущая роль: {UserControlScenario.FormatUserStatus(user.Status)}\r\n\r\n" +
                "Выберите новую роль:",
                _userData),
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    /// <summary>
    /// Применяет новую роль. SetUserStatusAsync сбрасывает Pending —
    /// тем самым закрывается заявка на повышение без отдельных кнопок одобрения.
    /// </summary>
    private async Task ApplyUserStatusAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (_userData == null || callbackQuery.Message == null)
            return;

        var parts = callbackQuery.Data!.Split('|');
        if (parts.Length < 3
            || !Guid.TryParse(parts[1], out var userId)
            || !int.TryParse(parts[2], out var statusCode)
            || !Enum.IsDefined(typeof(UserStatus), statusCode))
        {
            return;
        }

        var newStatus = (UserStatus)statusCode;

        if (userId == _userData.Id && newStatus != UserStatus.Admin)
        {
            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText("Нельзя понизить собственную роль.", _userData),
                cancellationToken: ct);
            return;
        }

        try
        {
            var user = await _botUsersService.GetUserByIdAsync(userId, ct);
            if (user == null)
                return;

            var oldStatus = user.Status;
            await _botUsersService.SetUserStatusAsync(userId, newStatus, ct);

            await _telegramBotClient.SendMessage(
                user.TelegramId,
                $"Ваша роль изменена: {UserControlScenario.FormatUserStatus(oldStatus)} → {UserControlScenario.FormatUserStatus(newStatus)}.",
                replyMarkup: ConstantData.CreateReplyKeyboardMarkup(
                    await _botUsersService.GetUserAsync(user.TelegramId, ct)),
                cancellationToken: ct);

            user = await _botUsersService.GetUserByIdAsync(userId, ct);
            if (user == null)
                return;

            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText(BuildUserDetailText(user), _userData),
                replyMarkup: BuildUserDetailKeyboard(user),
                cancellationToken: ct);
        }
        catch (InfraBotException ex)
        {
            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText(ex.Message, _userData),
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// /pending — ставит заявку в очередь (Pending = Pending).
    /// Уведомления админам не отправляются; заявка видна в /usercontrol и /pendingrequests.
    /// </summary>
    private async Task ElevationRequestAsync(Update update, CancellationToken ct)
    {
        if (_userData == null)
            return;

        var chatId = GetChatFromUpdate(update);

        try
        {
            await _botUsersService.RequestElevationAsync(_userData.Id, ct);

            await _telegramBotClient.SendMessage(
                chatId,
                ConstantData.ReplaceText(
                        "Запрос на повышение поставлен в очередь.",
                    _userData),
                replyMarkup: _replyKeyboardMarkup,
                cancellationToken: ct);
        }
        catch (InfraBotException ex)
        {
            await _telegramBotClient.SendMessage(
                chatId,
                ConstantData.ReplaceText(ex.Message, _userData),
                replyMarkup: _replyKeyboardMarkup,
                cancellationToken: ct);
        }
    }

    private List<KeyValuePair<string, string>> BuildUserButtonList(IReadOnlyList<BotUser> users)
    {
        var result = new List<KeyValuePair<string, string>>();

        foreach (var user in users.OrderBy(u => u.Username))
        {
            // ⏳ — активная заявка на повышение, иначе emoji текущей роли
            var prefix = user.Pending == UserPending.Pending ? "⏳ " : GetStatusEmoji(user.Status);
            var label = $"{prefix}{UserControlScenario.FormatUserLabel(user)}";
            var callback = $"{"showuserdetail"}|{user.Id}";
            result.Add(new KeyValuePair<string, string>(label, callback));
        }

        return result;
    }

    private static string GetStatusEmoji(UserStatus status) => status switch
    {
        UserStatus.Guest => "👤 ",
        UserStatus.Operator => "🛠 ",
        UserStatus.Admin => "👑 ",
        _ => ""
    };

    private static string BuildUserDetailText(BotUser user)
    {
        var text = $"Пользователь: {UserControlScenario.FormatUserLabel(user)}\r\n" +
                   $"Telegram ID: {user.TelegramId}\r\n" +
                   $"Роль: {UserControlScenario.FormatUserStatus(user.Status)}";

        if (user.Pending == UserPending.Pending && user.Status == UserStatus.Guest)
        {
            text += $"\r\nЗапрос на повышение: Guest → Operator";
        }

        return text;
    }

    /// <summary>Доступные действия в карточке: смена роли.</summary>
    private InlineKeyboardMarkup BuildUserDetailKeyboard(BotUser user)
    {
        var keyboard = new InlineKeyboardMarkup();
        keyboard.AddNewRow(
            InlineKeyboardButton.WithCallbackData("Роль пользователя", $"{"selectuserstatus"}|{user.Id}"));

        keyboard.AddNewRow(
            InlineKeyboardButton.WithCallbackData("К списку пользователей", $"{"listusers"}||{_currentUsersPage}"));

        return keyboard;
    }
}
