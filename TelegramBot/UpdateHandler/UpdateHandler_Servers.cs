using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Callback;
using InfraBot.Scenarios.Core;
using InfraBot.Scenarios.Tasks.JobRun;
using InfraBot.Scenarios.Tasks.Server;
using InfraBot.Scenarios.Tasks.User;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot
{
    internal partial class UpdateHandler
    {
        private async Task ShowServerDetailAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            if (_userData == null || callbackQuery.Message == null)
                return;

            var serverDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
            if (serverDto.ObjectID == null)
                return;

            var server = await _serversService.GetServerAsync(serverDto.ObjectID.Value, ct);
            if (server == null)
            {
                await _telegramBotClient.EditMessageText(
                    callbackQuery.Message.Chat,
                    callbackQuery.Message.MessageId,
                    ConstantData.ReplaceText("Сервер не найден", _userData),
                    cancellationToken: ct);
                return;
            }

            var detailText = new StringBuilder();
            detailText.AppendLine(server.ServerName);
            detailText.AppendLine($"IP: {server.IpAddress}");
            detailText.AppendLine($"WinRM: {server.WinRmPort}");

            if (server.SvcSamAccountId != Guid.Empty)
            {
                var svcAccount = await _svcAccountsService.GetAsync(server.SvcSamAccountId, ct);
                detailText.AppendLine($"УЗ WinRM: {svcAccount?.SamAccountName ?? "не найдена"}");
            }
            else
                detailText.AppendLine("УЗ WinRM: не задана");

            detailText.AppendLine($"Описание: {server.Description}");
            detailText.AppendLine($"Скриптов привязано: {server.ScriptRequirements.Count}");

            if (_userData.Status == UserStatus.Admin)
            {
                if (server.GrantedUserIds.Count == 0)
                    detailText.AppendLine("Доступ: никому не выдан");
                else
                {
                    var grantedLabels = new List<string>();
                    foreach (var userId in server.GrantedUserIds)
                    {
                        var grantedUser = await _botUsersService.GetUserByIdAsync(userId, ct);
                        grantedLabels.Add(UserControlScenario.FormatUserLabel(grantedUser));
                    }

                    detailText.AppendLine($"Доступ ({server.GrantedUserIds.Count}): {string.Join(", ", grantedLabels)}");
                }
            }
            else
            {
                detailText.AppendLine($"Доступно для запуска: {server.ScriptRequirements.Count} скрипт(ов)");
            }

            var backCallback = new PagedListCallbackDtoServers
            {
                Action = "listservers",
                ObjectID = null,
                Page = _currentPage
            };

            var inlineKeyboard = new InlineKeyboardMarkup();
            var canRunAny = server.ScriptRequirements.Count > 0;

            if (_userData!.Status is UserStatus.Operator or UserStatus.Admin && canRunAny)
            {
                inlineKeyboard.AddNewRow(
                    CreateButtonInline("Запуск скрипта", $"runjob|{server.Id}", KeyboardButtonStyle.Primary)
                );
            }

            if (_userData.Status == UserStatus.Admin)
            {
                inlineKeyboard.AddNewRow(
                    CreateButtonInline("Управление доступом", $"updateserver|{server.Id}|access")
                );
                inlineKeyboard.AddNewRow(
                    CreateButtonInline("Изменить параметры", $"updateserver|{server.Id}")
                );
                inlineKeyboard.AddNewRow(
                    CreateButtonInline("❌ Удалить", $"deleteserver|{server.Id}", KeyboardButtonStyle.Danger)
                );
                
            }

            inlineKeyboard.AddNewRow(CreateButtonInline("⬅️ К списку серверов", backCallback.ToString(),KeyboardButtonStyle.Success));

            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText(detailText.ToString(), _userData),
                replyMarkup: inlineKeyboard,
                cancellationToken: ct);
        }

        private async Task ShowServersListAsync(Update update, CallbackQuery? callbackQuery, CancellationToken ct)
        {
            if (_userData == null)
                return;

            var chat = callbackQuery?.Message?.Chat ?? GetChatFromUpdate(update);
            var servers = await _serversService.GetAccessibleServersAsync(_userData, ct);

            var serverButtons = new List<KeyValuePair<string, string>>();

            foreach (var server in servers)
            {
                var buttonText = server.ServerName;
                if (string.IsNullOrEmpty(buttonText))
                    continue;

                var callbackDto = CallbackDtoIdObject.FromString($"showserverdetail|{server.Id}");
                serverButtons.Add(new KeyValuePair<string, string>(buttonText, callbackDto.ToString()));
            }

            if (serverButtons.Count == 0)
            {
                await ReplaceOrSendMessage(ConstantData.ReplaceText("У вас нет доступных серверов", _userData), callbackQuery?.Message, chat, _replyKeyboardMarkup, ct);
                return;
            }

            if (callbackQuery?.Data != null)
            {
                try
                {
                    var pagedCallback = PagedListCallbackDtoServers.FromString(callbackQuery.Data);
                    _currentPage = pagedCallback.Page;
                }
                catch
                {
                    _currentPage = 0;
                }
            }

            var pageListDto = new PagedListCallbackDtoServers
            {
                Action = "listservers",
                ObjectID = null,
                Page = _currentPage
            };

            var (inlineKeyboard, currentPage, totalPages) = BuildPagedButtons(serverButtons, pageListDto);
            _currentPage = currentPage;

            await ReplaceOrSendMessage(
                ConstantData.ReplaceText($"Доступные сервера\r\nСтраница {currentPage + 1} из {totalPages}",
                _userData), callbackQuery?.Message, chat, inlineKeyboard, ct);
        }

        private async Task StartDeleteServerScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var serverDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
            if (serverDto.ObjectID == null)
                return;

            var deleteServerContext = new ScenarioContext(ScenarioType.DeleteServer);
            deleteServerContext.Data["ServerId"] = serverDto.ObjectID.Value;
            var deleteServerScenario = new DeleteServerScenario(_serversService, _jobRunsService, _botUsersService);
            _scenarios = _scenarios.Append(deleteServerScenario).ToList();
            await ProcessScenarioAsync(deleteServerContext, update, ct);
        }

        private async Task StartUpdateServerScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var parts = callbackQuery.Data!.Split('|');
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var serverId))
                return;

            var updateServerContext = new ScenarioContext(ScenarioType.UpdateServer);
            updateServerContext.Data["ServerId"] = serverId;
            if (parts.Length > 2 && parts[2] == "access")
                updateServerContext.Data["DirectAccess"] = true;

            var updateServerScenario = new UpdateServerScenario(_serversService, _botUsersService, _svcAccountsService, _scriptsService);
            _scenarios = _scenarios.Append(updateServerScenario).ToList();
            await ProcessScenarioAsync(updateServerContext, update, ct);
        }

        private async Task StartRunJobScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            if (_userData == null)
                return;

            var parts = callbackQuery.Data!.Split('|');
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var serverId))
                return;

            var accessibleServers = await _serversService.GetAccessibleServersAsync(_userData, ct);
            if (!accessibleServers.Any(s => s.Id == serverId))
                return;

            var runJobContext = new ScenarioContext(ScenarioType.RunJob);
            runJobContext.Data["ServerId"] = serverId;
            var runJobScenario = new RunJobScenario(_serversService, _scriptsService, _jobRunsService, _jobRunExe, _botUsersService);
            _scenarios = _scenarios.Append(runJobScenario).ToList();
            await ProcessScenarioAsync(runJobContext, update, ct);
        }
    }
}
