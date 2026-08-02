using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Callback;
using InfraBot.Scenarios.Core;
using InfraBot.Scenarios.Tasks.SvcSamAccount;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    private int _currentSvcAccountsPage;

    private async Task ShowSvcAccountDetailAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (_userData == null || callbackQuery.Message == null)
            return;

        var accountDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (accountDto.ObjectID == null)
            return;

        var account = await _svcAccountsService.GetAsync(accountDto.ObjectID.Value, ct);
        if (account == null)
        {
            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText("Учётная запись не найдена", _userData),
                cancellationToken: ct);
            return;
        }

        var linkedServers = await _serversService.GetServersBySvcAccountAsync(account.Id, ct);
        var detailText = $"Учётная запись: {account.SamAccountName}";
        if (linkedServers.Count > 0)
            detailText += $"\r\nСерверы: {string.Join(", ", linkedServers.Select(s => s.ServerName))}";

        var backCallback = new PagedListCallbackDtoSvcAccounts
        {
            Action = "listsvcaccounts",
            ObjectID = null,
            Page = _currentSvcAccountsPage
        };

        var inlineKeyboard = new InlineKeyboardMarkup();
        if (_userData.Status == UserStatus.Admin)
        {
            inlineKeyboard.AddNewRow(
                InlineKeyboardButton.WithCallbackData(
                    "Пароль",
                    $"updatesvcaccount|{account.Id}"));
            inlineKeyboard.AddNewRow(
                InlineKeyboardButton.WithCallbackData(
                    "❌ Удалить",
                    $"deletesvcaccount|{account.Id}"));
        }

        inlineKeyboard.AddNewRow(
            InlineKeyboardButton.WithCallbackData("⬅️ К списку", backCallback.ToString()));

        await _telegramBotClient.EditMessageText(
            callbackQuery.Message.Chat,
            callbackQuery.Message.MessageId,
            ConstantData.ReplaceText(detailText, _userData),
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    private async Task ShowSvcAccountsListAsync(Update update, CallbackQuery? callbackQuery, CancellationToken ct)
    {
        if (_userData == null)
            return;

        var chat = callbackQuery?.Message?.Chat ?? GetChatFromUpdate(update);
        var accounts = await _svcAccountsService.GetAllAsync(ct);

        var accountButtons = new List<KeyValuePair<string, string>>();
        foreach (var account in accounts.OrderBy(a => a.SamAccountName))
        {
            var callbackDto = CallbackDtoIdObject.FromString($"showsvcaccountdetail|{account.Id}");
            accountButtons.Add(new KeyValuePair<string, string>(account.SamAccountName, callbackDto.ToString()));
        }

        if (accountButtons.Count == 0)
        {
            await ReplaceOrSendMessage(
                ConstantData.ReplaceText("Нет учётных записей WinRM", _userData),
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
                var pagedCallback = PagedListCallbackDtoSvcAccounts.FromString(callbackQuery.Data);
                _currentSvcAccountsPage = pagedCallback.Page;
            }
            catch
            {
                _currentSvcAccountsPage = 0;
            }
        }

        var pageListDto = new PagedListCallbackDtoSvcAccounts
        {
            Action = "listsvcaccounts",
            ObjectID = null,
            Page = _currentSvcAccountsPage
        };

        var (inlineKeyboard, currentPage, totalPages) = BuildPagedButtons(accountButtons, pageListDto);
        _currentSvcAccountsPage = currentPage;

        await ReplaceOrSendMessage(
            ConstantData.ReplaceText(
                $"WinRM УЗ\r\nСтраница {currentPage + 1} из {totalPages}",
                _userData),
            callbackQuery?.Message,
            chat,
            inlineKeyboard,
            ct);
    }

    private async Task StartUpdateSvcAccountScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var accountDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (accountDto.ObjectID == null)
            return;

        var updateSvcAccountContext = new ScenarioContext(ScenarioType.UpdateSvcSamAccount);
        updateSvcAccountContext.Data["SvcSamAccountId"] = accountDto.ObjectID.Value;
        var updateSvcAccountScenario = new UpdateSvcSamAccountScenario(_svcAccountsService, _botUsersService);
        _scenarios = _scenarios.Append(updateSvcAccountScenario).ToList();
        await ProcessScenarioAsync(updateSvcAccountContext, update, ct);
    }

    private async Task StartDeleteSvcAccountScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var accountDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (accountDto.ObjectID == null)
            return;

        var deleteSvcAccountContext = new ScenarioContext(ScenarioType.DeleteSvcSamAccount);
        deleteSvcAccountContext.Data["SvcSamAccountId"] = accountDto.ObjectID.Value;
        var deleteSvcAccountScenario = new DeleteSvcSamAccountScenario(_svcAccountsService, _serversService, _botUsersService);
        _scenarios = _scenarios.Append(deleteSvcAccountScenario).ToList();
        await ProcessScenarioAsync(deleteSvcAccountContext, update, ct);
    }
}
