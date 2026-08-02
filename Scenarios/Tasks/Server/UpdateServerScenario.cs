using InfraBot.Core.Interface.Services;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Helpers;
using InfraBot.Scenarios.Core;
using InfraBot.Scenarios.Tasks.User;
using InfraBot.TelegramBot;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Server;

/// <summary>
/// Изменение сервера из карточки: выбор атрибута → ввод/выбор → сохранение.
/// </summary>
internal sealed class UpdateServerScenario : IScenario
{
    private const int PageSize = 8;
    private const int Columns = 2;

    private const string AttrAction = "updateserverattr";
    private const string ScriptsPageAction = "updateserverscripts";
    private const string ScriptSelectPrefix = "updateserverscript|";
    private const string SvcSelectPrefix = "updateserversvc|";
    private const string AccessTogglePrefix = "updateserveraccess|toggle|";
    private const string AccessDoneCallback = "updateserveraccess|done";
    private const string DoneCallback = "updateserverdone";

    private static readonly int[] ValidWinRmPorts = [5985, 5986];

    private readonly IServerService _servers;
    private readonly IBotUserService _users;
    private readonly ISvcSamAccountService _svcAccounts;
    private readonly IScriptService _scripts;

    public UpdateServerScenario(
        IServerService servers,
        IBotUserService users,
        ISvcSamAccountService svcAccounts,
        IScriptService scripts)
    {
        _servers = servers;
        _users = users;
        _svcAccounts = svcAccounts;
        _scripts = scripts;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.UpdateServer;

    public async Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
    {
        var user = await _users.GetUserAsync(UpdateHandler.GetUserIdFromUpdate(update), ct);
        if (user == null)
            return ScenarioResult.Completed;

        var chat = UpdateHandler.GetChatFromUpdate(update);
        var defaultKeyboard = ConstantData.CreateReplyKeyboardMarkup(user);
        var cancelKeyboard = ConstantData.CreateCancelKeyboard();
        var inputText = update.Message?.Text?.Trim() ?? string.Empty;
        var callbackData = update.CallbackQuery?.Data;

        if (inputText == ConstantData.Cancel)
            context.CurrentStep = "Cancel";

        switch (context.CurrentStep)
        {
            case null:
                if (!context.Data.TryGetValue("ServerId", out var menuIdObj) || menuIdObj is not Guid menuServerId)
                    return ScenarioResult.Completed;

                var menuServer = await _servers.GetServerAsync(menuServerId, ct);
                if (menuServer == null)
                {
                    await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Сервер не найден.", user), null, ct);
                    return ScenarioResult.Completed;
                }

                context.Data["ServerName"] = menuServer.ServerName;

                if (context.Data.TryGetValue("DirectAccess", out var directAccess) && directAccess is true)
                    return await ShowUserAccessListAsync(bot, context, update, chat, user, ct);

                var menuKeyboard = new InlineKeyboardMarkup();
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("WinRM УЗ", $"{AttrAction}|src"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("IP-адрес", $"{AttrAction}|ip"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Описание", $"{AttrAction}|description"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("WinRM порт", $"{AttrAction}|winrm"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Доступные скрипты", $"{AttrAction}|scripts"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Доступ пользователей", $"{AttrAction}|access"));
                await EditOrSendAsync(
                    bot,
                    update,
                    chat,
                    ConstantData.ReplaceText($"Выберите атрибут для изменения:\r\nСервер: «{menuServer.ServerName}»", user),
                    menuKeyboard,
                    ct);
                context.CurrentStep = "SelectAttribute";
                return ScenarioResult.Transition;

            case "SelectAttribute":
                if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith($"{AttrAction}|", StringComparison.Ordinal))
                    return ScenarioResult.Transition;

                switch (callbackData[(AttrAction.Length + 1)..])
                {
                    case "src":
                        var accounts = await _svcAccounts.GetAllAsync(ct);
                        if (accounts.Count == 0)
                        {
                            await EditOrSendAsync(
                                bot, update, chat,
                                ConstantData.ReplaceText("Нет доступных учётных записей.", user),
                                null, ct);
                            return ScenarioResult.Completed;
                        }

                        var srcKeyboard = new InlineKeyboardMarkup();
                        foreach (var svcAccount in accounts.OrderBy(a => a.SamAccountName))
                        {
                            srcKeyboard.AddNewRow(
                                InlineKeyboardButton.WithCallbackData(
                                    svcAccount.SamAccountName,
                                    $"{SvcSelectPrefix}{svcAccount.Id}"));
                        }

                        await EditOrSendAsync(
                            bot, update, chat,
                            ConstantData.ReplaceText("Выберите учётную запись для WinRM:", user),
                            srcKeyboard, ct);
                        context.CurrentStep = "SelectSrc";
                        return ScenarioResult.Transition;

                    case "description":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите новое описание сервера:", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterDescription";
                        return ScenarioResult.Transition;

                    case "ip":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите новый IP-адрес сервера:", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterIp";
                        return ScenarioResult.Transition;

                    case "winrm":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите порт WinRM (дефолтные 5985 или 5986):", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterWinRm";
                        return ScenarioResult.Transition;

                    case "scripts":
                        return await ShowScriptsListAsync(bot, context, update, chat, user, 0, ct);

                    case "access":
                        return await ShowUserAccessListAsync(bot, context, update, chat, user, ct);

                    default:
                        return ScenarioResult.Transition;
                }

            case "SelectSrc":
                if (string.IsNullOrEmpty(callbackData))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ServerId", out var srcIdObj) || srcIdObj is not Guid srcServerId)
                    return ScenarioResult.Completed;

                var srcServer = await _servers.GetServerAsync(srcServerId, ct);
                if (srcServer == null)
                {
                    await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Сервер не найден.", user), null, ct);
                    return ScenarioResult.Completed;
                }

                if (!callbackData.StartsWith(SvcSelectPrefix, StringComparison.Ordinal)
                    || !Guid.TryParse(callbackData[SvcSelectPrefix.Length..], out var accountId))
                {
                    return ScenarioResult.Transition;
                }

                var selectedAccount = await _svcAccounts.GetAsync(accountId, ct);
                if (selectedAccount == null)
                {
                    await bot.SendMessage(chat, "Учётная запись не найдена.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                srcServer.SvcSamAccountId = selectedAccount.Id;
                await _servers.UpdateServerAsync(srcServer, ct);
                await EditOrSendAsync(
                    bot, update, chat,
                    ConstantData.ReplaceText($"Для сервера «{srcServer.ServerName}» выбрана УЗ «{selectedAccount.SamAccountName}».", user),
                    null, ct);
                return ScenarioResult.Completed;

            case "EnterDescription":
                if (string.IsNullOrEmpty(inputText))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ServerId", out var descIdObj) || descIdObj is not Guid descServerId)
                    return ScenarioResult.Completed;

                var descServer = await _servers.GetServerAsync(descServerId, ct);
                if (descServer == null)
                {
                    await bot.SendMessage(chat, "Сервер не найден.", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                descServer.Description = inputText;
                await _servers.UpdateServerAsync(descServer, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Описание сервера «{descServer.ServerName}» обновлено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "EnterIp":
                if (string.IsNullOrWhiteSpace(inputText))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ServerId", out var ipIdObj) || ipIdObj is not Guid ipServerId)
                    return ScenarioResult.Completed;

                var ipServer = await _servers.GetServerAsync(ipServerId, ct);
                if (ipServer == null)
                {
                    await bot.SendMessage(chat, "Сервер не найден.", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                ipServer.IpAddress = inputText.Trim();
                await _servers.UpdateServerAsync(ipServer, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"IP-адрес сервера «{ipServer.ServerName}» обновлён: {ipServer.IpAddress}.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "EnterWinRm":
                if (string.IsNullOrEmpty(inputText))
                    return ScenarioResult.Transition;

                //inputText
                
                if (!int.TryParse(inputText, out var port) || !ValidWinRmPorts.Contains(port))
                {
                    await bot.SendMessage(
                        chat,
                        "Недопустимый порт. Введите 5985 (HTTP) или 5986 (HTTPS):",
                        replyMarkup: cancelKeyboard,
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                if (!context.Data.TryGetValue("ServerId", out var winRmIdObj) || winRmIdObj is not Guid winRmServerId)
                    return ScenarioResult.Completed;

                var winRmServer = await _servers.GetServerAsync(winRmServerId, ct);
                if (winRmServer == null)
                {
                    await bot.SendMessage(chat, "Сервер не найден.", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                winRmServer.WinRmPort = port;
                await _servers.UpdateServerAsync(winRmServer, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Порт WinRM сервера «{winRmServer.ServerName}» установлен: {port}.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "SelectScripts":
                if (string.IsNullOrEmpty(callbackData))
                    return ScenarioResult.Transition;

                if (callbackData == DoneCallback)
                {
                    await EditOrSendAsync(
                        bot, update, chat,
                        ConstantData.ReplaceText("Набор скриптов обновлён.", user),
                        null, ct);
                    return ScenarioResult.Completed;
                }

                if (callbackData.StartsWith(ScriptsPageAction, StringComparison.Ordinal))
                {
                    var parts = callbackData.Split('|');
                    var targetPage = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 0;
                    return await ShowScriptsListAsync(bot, context, update, chat, user, targetPage, ct);
                }

                if (callbackData.StartsWith(ScriptSelectPrefix, StringComparison.Ordinal)
                    && Guid.TryParse(callbackData[ScriptSelectPrefix.Length..], out var scriptId)
                    && context.Data.TryGetValue("ServerId", out var scriptsIdObj)
                    && scriptsIdObj is Guid scriptsServerId)
                {
                    if (await _servers.GetServerAsync(scriptsServerId, ct) is { } scriptsServer)
                    {
                        if (scriptsServer.ScriptRequirements.Contains(scriptId))
                            await _servers.RemoveScriptFromServerAsync(scriptsServerId, scriptId, ct);
                        else
                            await _servers.AddScriptToServerAsync(scriptsServerId, scriptId, ct);
                    }
                }

                var currentPage = context.Data.TryGetValue("ScriptsPage", out var pageObj) && pageObj is int page ? page : 0;
                return await ShowScriptsListAsync(bot, context, update, chat, user, currentPage, ct);

            case "SelectAccess":
                if (string.IsNullOrEmpty(callbackData))
                    return ScenarioResult.Transition;

                if (callbackData == AccessDoneCallback)
                {
                    await EditOrSendAsync(
                        bot, update, chat,
                        ConstantData.ReplaceText("Управление доступом завершено.", user),
                        null, ct);
                    return ScenarioResult.Completed;
                }

                if (callbackData.StartsWith(AccessTogglePrefix, StringComparison.Ordinal)
                    && Guid.TryParse(callbackData[AccessTogglePrefix.Length..], out var targetUserId)
                    && context.Data.TryGetValue("ServerId", out var accessIdObj)
                    && accessIdObj is Guid accessServerId)
                {
                    var accessServer = await _servers.GetServerAsync(accessServerId, ct);
                    if (accessServer == null)
                    {
                        await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Сервер не найден.", user), null, ct);
                        return ScenarioResult.Completed;
                    }

                    if (accessServer.GrantedUserIds.Contains(targetUserId))
                        await _servers.RevokeAccessAsync(accessServerId, targetUserId, ct);
                    else
                        await _servers.GrantAccessAsync(accessServerId, targetUserId, ct);
                }

                return await ShowUserAccessListAsync(bot, context, update, chat, user, ct);

            case "Cancel":
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Изменение сервера отменено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }

    private async Task<ScenarioResult> ShowScriptsListAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        Chat chat,
        Entities.BotUser user,
        int page,
        CancellationToken ct)
    {
        if (!context.Data.TryGetValue("ServerId", out var idObj) || idObj is not Guid serverId)
            return ScenarioResult.Completed;

        var server = await _servers.GetServerAsync(serverId, ct);
        if (server == null)
        {
            await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Сервер не найден.", user), null, ct);
            return ScenarioResult.Completed;
        }

        var scripts = (await _scripts.GetAllScriptsAsync(ct)).OrderBy(s => s.Name).ToList();
        if (scripts.Count == 0)
        {
            await EditOrSendAsync(
                bot, update, chat,
                ConstantData.ReplaceText("Нет скриптов для выбора.", user),
                null, ct);
            return ScenarioResult.Completed;
        }

        var scriptButtons = scripts
            .Select(script =>
            {
                var prefix = server.ScriptRequirements.Contains(script.Id) ? "✅ " : string.Empty;
                return new KeyValuePair<string, string>(
                    $"{prefix}{script.Name}",
                    $"{ScriptSelectPrefix}{script.Id}");
            })
            .ToList();

        var (keyboard, currentPage, totalPages) = BuildPagedButtons(scriptButtons, page);
        keyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("✔️ Готово", DoneCallback));

        await EditOrSendAsync(
            bot,
            update,
            chat,
            ConstantData.ReplaceText(
                $"Выберите скрипты для сервера «{server.ServerName}»\r\nСтраница {currentPage + 1} из {totalPages}",
                user),
            keyboard,
            ct);

        context.CurrentStep = "SelectScripts";
        context.Data["ScriptsPage"] = currentPage;
        return ScenarioResult.Transition;
    }

    private async Task<ScenarioResult> ShowUserAccessListAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        Chat chat,
        Entities.BotUser user,
        CancellationToken ct)
    {
        if (!context.Data.TryGetValue("ServerId", out var idObj) || idObj is not Guid serverId)
            return ScenarioResult.Completed;

        var server = await _servers.GetServerAsync(serverId, ct);
        if (server == null)
        {
            await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Сервер не найден.", user), null, ct);
            return ScenarioResult.Completed;
        }

        var allUsers = await _users.GetAllUsersAsync(ct);
        var keyboard = new InlineKeyboardMarkup();

        foreach (var target in allUsers.OrderBy(u => u.Username))
        {
            var granted = server.GrantedUserIds.Contains(target.Id);
            var prefix = granted ? "✅ " : "⬜ ";
            var label = $"{prefix}{UserControlScenario.FormatUserLabel(target)}";
            keyboard.AddNewRow(
                InlineKeyboardButton.WithCallbackData(label, $"{AccessTogglePrefix}{target.Id}"));
        }

        keyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("✔️ Готово", AccessDoneCallback));

        await EditOrSendAsync(
            bot,
            update,
            chat,
            ConstantData.ReplaceText(
                $"Доступ к серверу «{server.ServerName}»\r\n" +
                "Нажмите на пользователя, чтобы выдать или отозвать доступ:",
                user),
            keyboard,
            ct);

        context.CurrentStep = "SelectAccess";
        return ScenarioResult.Transition;
    }

    private (InlineKeyboardMarkup Keyboard, int CurrentPage, int TotalPages) BuildPagedButtons(
        IReadOnlyList<KeyValuePair<string, string>> callbackData,
        int page)
    {
        var totalPages = Math.Max(1, (callbackData.Count + PageSize - 1) / PageSize);
        var currentPage = Math.Clamp(page, 0, totalPages - 1);
        var keyboard = new InlineKeyboardMarkup();

        var itemsOnPage = callbackData.GetBatchByNumber(PageSize, currentPage)?
            .Cast<KeyValuePair<string, string>>()
            .ToList() ?? [];

        for (var i = 0; i < itemsOnPage.Count; i += Columns)
        {
            var row = new List<InlineKeyboardButton>();
            for (var j = i; j < Math.Min(i + Columns, itemsOnPage.Count); j++)
            {
                var item = itemsOnPage[j];
                row.Add(InlineKeyboardButton.WithCallbackData(item.Key, item.Value));
            }

            keyboard.AddNewRow(row.ToArray());
        }

        if (totalPages > 1)
        {
            var navButtons = new List<InlineKeyboardButton>();
            AddPageButton(navButtons, currentPage, totalPages, -1, "⬅️");
            AddPageButton(navButtons, currentPage, totalPages, 1, "➡️");
            AddPageButton(navButtons, currentPage, totalPages, -10, "◀ -10");
            AddPageButton(navButtons, currentPage, totalPages, 10, "+10 ▶");
            if (navButtons.Count > 0)
                keyboard.AddNewRow(navButtons.ToArray());
        }

        return (keyboard, currentPage, totalPages);
    }

    private void AddPageButton(List<InlineKeyboardButton> buttons, int currentPage, int totalPages, int delta, string text)
    {
        var targetPage = currentPage + delta;
        if (targetPage < 0 || targetPage >= totalPages)
            return;

        buttons.Add(InlineKeyboardButton.WithCallbackData(text, $"{ScriptsPageAction}|{targetPage}"));
    }

    private static async Task EditOrSendAsync(
        ITelegramBotClient bot,
        Update update,
        Chat chat,
        string text,
        InlineKeyboardMarkup? keyboard,
        CancellationToken ct)
    {
        if (update.CallbackQuery?.Message != null)
        {
            await bot.EditMessageText(
                chat,
                update.CallbackQuery.Message.MessageId,
                text,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        else
        {
            await bot.SendMessage(chat, text, replyMarkup: keyboard, cancellationToken: ct);
        }
    }
}
