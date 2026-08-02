using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Helpers;
using InfraBot.Infrastracture.Callback;
using InfraBot.Infrastracture.Services;
using InfraBot.Scenarios.Core;
using InfraBot.Scenarios.Tasks.Script;
using InfraBot.Scenarios.Tasks.Server;
using InfraBot.Scenarios.Tasks.SvcSamAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    public async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(callbackQuery.Data))
            return;

        _userData = await _botUsersService.GetUserAsync(GetUserIdFromUpdate(update), ct);
        if (_userData == null)
            return;

        // Активный сценарий перехватывает callback
        var activeScenario = await _scenarioContextRepository.GetContext(_userData.TelegramId, ct);
        if (activeScenario != null)
        {
            await ProcessScenarioAsync(activeScenario, update, ct);
            await _telegramBotClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            return;
        }

        var callbackDto = CallbackDto.FromString(callbackQuery.Data);

        switch (callbackDto.Action)
        {
            case "listservers":
                await ShowServersListAsync(update, callbackQuery, ct);
                break;
            case "showserverdetail":
                await ShowServerDetailAsync(update, callbackQuery, ct);
                break;
            case "deleteserver":
                await StartDeleteServerScenarioAsync(update, callbackQuery, ct);
                break;
            case "updateserver":
                await StartUpdateServerScenarioAsync(update, callbackQuery, ct);
                break;
            case "runjob":
                await StartRunJobScenarioAsync(update, callbackQuery, ct);
                break;
            case "listscripts":
                await ShowScriptsListAsync(update, callbackQuery, ct);
                break;
            case "showscriptdetail":
                await ShowScriptDetailAsync(update, callbackQuery, ct);
                break;
            case "updatescript":
                await StartUpdateScriptScenarioAsync(update, callbackQuery, ct);
                break;
            case "deletescript":
                await StartDeleteScriptScenarioAsync(update, callbackQuery, ct);
                break;
            case "listsvcaccounts":
                await ShowSvcAccountsListAsync(update, callbackQuery, ct);
                break;
            case "showsvcaccountdetail":
                await ShowSvcAccountDetailAsync(update, callbackQuery, ct);
                break;
            case "updatesvcaccount":
                await StartUpdateSvcAccountScenarioAsync(update, callbackQuery, ct);
                break;
            case "deletesvcaccount":
                await StartDeleteSvcAccountScenarioAsync(update, callbackQuery, ct);
                break;
            case "listusers":
                await ShowUsersListAsync(update, callbackQuery, pendingOnly: false, ct);
                break;
            case "listpendingusers":
                await ShowUsersListAsync(update, callbackQuery, pendingOnly: true, ct);
                break;
            case "showuserdetail":
                await ShowUserDetailAsync(update, callbackQuery, ct);
                break;
            case "selectuserstatus":
                await ShowUserStatusPickerAsync(update, callbackQuery, ct);
                break;
            case "setuserstatus":
                await ApplyUserStatusAsync(update, callbackQuery, ct);
                break;
        }

        await _telegramBotClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }

    public Task OnUnknown(Update update)
    {
        return Task.CompletedTask;
    }

    private async Task OnMessage(Update update, CancellationToken ct)
    {
        var text = GetMessageFromUpdate(update)?.Trim();
        var chatId = GetChatFromUpdate(update);
        if (string.IsNullOrEmpty(text))
            return;

        _userData = await _botUsersService.GetUserAsync(GetUserIdFromUpdate(update), ct);
        var telegramUserId = GetUserIdFromUpdate(update);

        if (_userData?.Status != UserStatus.Admin)
            _adminModuleActive.Remove(telegramUserId);

        _replyKeyboardMarkup = _userData?.Status == UserStatus.Admin && _adminModuleActive.GetValueOrDefault(telegramUserId)
            ? ConstantData.CreateAdminModuleKeyboard()
            : ConstantData.CreateReplyKeyboardMarkup(_userData);

        if (text == ConstantData.Cancel)
        {
            var userId = GetUserIdFromUpdate(update);
            var hadScenario = await _scenarioContextRepository.GetContext(userId, ct) != null;
            if (hadScenario)
            {
                await _scenarioContextRepository.ResetContext(userId, ct);
                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.ReplaceText("Сценарий отменён.", _userData),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                return;
            }

            if (_userData?.Status == UserStatus.Admin && _adminModuleActive.GetValueOrDefault(userId))
            {
                _adminModuleActive[userId] = false;
                _replyKeyboardMarkup = ConstantData.CreateReplyKeyboardMarkup(_userData);
                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.ReplaceText("Выход из админ модуля.", _userData),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                return;
            }

            await _telegramBotClient.SendMessage(
                chatId,
                ConstantData.ReplaceText("Нет активного сценария.", _userData),
                replyMarkup: _replyKeyboardMarkup,
                cancellationToken: ct);
            return;
        }

        if (_userData != null)
        {
            var activeScenario = await _scenarioContextRepository.GetContext(_userData.TelegramId, ct);
            if (activeScenario != null)
            {
                await ProcessScenarioAsync(activeScenario, update, ct);
                return;
            }
        }

        switch (text)
        {
            case ConstantData.Start:
                if (_userData == null)
                {
                    _userData = await _botUsersService.RegisterUserAsync(update.Message.From.Id,
                                                                    update.Message.From.Username,
                                                                    ct
                                                                    );
                    _replyKeyboardMarkup = ConstantData.CreateReplyKeyboardMarkup(_userData);
                    await _telegramBotClient.SendMessage(chatId,
                                                ConstantData.ReplaceText($"Доступны новые команды\r\n {ConstantData.HelpData(_userData)}", _userData),
                                                replyMarkup: _replyKeyboardMarkup,
                                                cancellationToken: ct);
                }
                break;

            case ConstantData.Pending:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                await ElevationRequestAsync(update, ct);
                break;

            case ConstantData.ListServers:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _currentPage = 0;
                await ShowServersListAsync(update, null, ct);
                break;

            case ConstantData.ListScripts:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _currentScriptsPage = 0;
                await ShowScriptsListAsync(update, null, ct);
                break;

            case ConstantData.PendingRequests:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _currentUsersPage = 0;
                await ShowUsersListAsync(update, null, pendingOnly: true, ct);
                break;

            case ConstantData.AddServer:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                var addServerContext = new ScenarioContext(ScenarioType.Server);
                var addServerScenario = new AddServerScenario(_serversService, _botUsersService, _svcAccountsService);
                _scenarios = _scenarios.Append(addServerScenario).ToList();
                await ProcessScenarioAsync(addServerContext, update, ct);
                break;

            case ConstantData.AddScript:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                var addScriptContext = new ScenarioContext(ScenarioType.Script);
                var addScriptScenario = new AddScriptScenario(_scriptsService, _botUsersService);
                _scenarios = _scenarios.Append(addScriptScenario).ToList();
                await ProcessScenarioAsync(addScriptContext, update, ct);
                break;

            case ConstantData.ListSvcAccounts:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _currentSvcAccountsPage = 0;
                await ShowSvcAccountsListAsync(update, null, ct);
                break;

            case ConstantData.AddSvcAccount:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                var addSvcAccountContext = new ScenarioContext(ScenarioType.SvcSamAccount);
                var addSvcAccountScenario = new AddSvcSamAccountScenario(_svcAccountsService, _botUsersService);
                _scenarios = _scenarios.Append(addSvcAccountScenario).ToList();
                await ProcessScenarioAsync(addSvcAccountContext, update, ct);
                break;

            case ConstantData.UserControl:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _currentUsersPage = 0;
                await ShowUsersListAsync(update, null, pendingOnly: false, ct);
                break;

            case ConstantData.Help:
                if (_userData == null)
                {
                    await _telegramBotClient.SendMessage(
                        chatId,
                        $"Доступные команды:\r\n{ConstantData.Start} - Регистрация и авторизация",
                        replyMarkup: _replyKeyboardMarkup,
                        cancellationToken: ct);
                    break;
                }

                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.ReplaceText($"Доступные команды:\r\n{ConstantData.HelpData(_userData)}", _userData),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                break;

            case ConstantData.AdminControl:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (!ConstantData.CommandsDictionary[text].Levels.Contains(_userData!.Status))
                {
                    await SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                _adminModuleActive[telegramUserId] = true;
                _replyKeyboardMarkup = ConstantData.CreateAdminModuleKeyboard();
                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.ReplaceText("Запущен админ модуль", _userData),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                break;

            case ConstantData.Info:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                var infoReport = await _userInfoReportService.BuildAsync(_userData!, ct);
                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.InfoData(_userData!, infoReport),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                break;

            case ConstantData.About:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                await _telegramBotClient.SendMessage(
                    chatId,
                    ConstantData.ReplaceText(ConstantData.AboutData(), _userData),
                    replyMarkup: _replyKeyboardMarkup,
                    cancellationToken: ct);
                break;
            case ConstantData.Report:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                await SendJobReportAsync(allJobs: false, chatId, ct);
                break;

            case ConstantData.ReportAll:
                if (await CheckAnonimus(_userData, chatId, _replyKeyboardMarkup, ct)) break;
                if (_userData!.Status != UserStatus.Admin)
                {
                    await _telegramBotClient.SendMessage(
                        chatId,
                        ConstantData.ReplaceText("Команда доступна только администратору.", _userData),
                        replyMarkup: _replyKeyboardMarkup,
                        cancellationToken: ct);
                    break;
                }
                await SendJobReportAsync(allJobs: true, chatId, ct);
                break;
            default:
                await SendErrorComand(chatId, text, _userData!, _replyKeyboardMarkup, ct);
                break;
        }
    }
}
