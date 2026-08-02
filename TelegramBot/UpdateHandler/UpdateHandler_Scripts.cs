using InfraBot.Entities;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Callback;
using InfraBot.Scenarios.Core;
using InfraBot.Scenarios.Tasks.Script;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    private int _currentScriptsPage;

    private async Task ShowScriptDetailAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        if (_userData == null || callbackQuery.Message == null)
            return;

        var scriptDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (scriptDto.ObjectID == null)
            return;

        var script = await _scriptsService.GetScriptAsync(scriptDto.ObjectID.Value, ct);
        if (script == null)
        {
            await _telegramBotClient.EditMessageText(
                callbackQuery.Message.Chat,
                callbackQuery.Message.MessageId,
                ConstantData.ReplaceText("Скрипт не найден", _userData),
                cancellationToken: ct);
            return;
        }

        var detailText = BuildScriptDetailText(script);

        var backCallback = new PagedListCallbackDtoScripts
        {
            Action = "listscripts",
            ObjectID = null,
            Page = _currentScriptsPage
        };

        var inlineKeyboard = new InlineKeyboardMarkup();
        inlineKeyboard.AddNewRow(
            CreateButtonInline("Изменить", $"updatescript|{script.Id}")
        );
        inlineKeyboard.AddNewRow(
            CreateButtonInline("❌ Удалить", $"deletescript|{script.Id}", KeyboardButtonStyle.Danger)
        );
        inlineKeyboard.AddNewRow(
            CreateButtonInline("⬅️ К списку", backCallback.ToString(), KeyboardButtonStyle.Success)
        );

        await _telegramBotClient.EditMessageText(
            callbackQuery.Message.Chat,
            callbackQuery.Message.MessageId,
            ConstantData.ReplaceText(detailText, _userData),
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    private async Task ShowScriptsListAsync(Update update, CallbackQuery? callbackQuery, CancellationToken ct)
    {
        if (_userData == null)
            return;

        var chat = callbackQuery?.Message?.Chat ?? GetChatFromUpdate(update);
        var scripts = await _scriptsService.GetAllScriptsAsync(ct);

        var scriptButtons = new List<KeyValuePair<string, string>>();
        foreach (var script in scripts.OrderBy(s => s.Name))
        {
            var callbackDto = CallbackDtoIdObject.FromString($"{"showscriptdetail"}|{script.Id}");
            scriptButtons.Add(new KeyValuePair<string, string>(script.Name, callbackDto.ToString()));
        }

        if (scriptButtons.Count == 0)
        {
            await ReplaceOrSendMessage(
                ConstantData.ReplaceText("Нет скриптов", _userData),
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
                var pagedCallback = PagedListCallbackDtoScripts.FromString(callbackQuery.Data);
                _currentScriptsPage = pagedCallback.Page;
            }
            catch
            {
                _currentScriptsPage = 0;
            }
        }

        var pageListDto = new PagedListCallbackDtoScripts
        {
            Action = "listscripts",
            ObjectID = null,
            Page = _currentScriptsPage
        };

        var (inlineKeyboard, currentPage, totalPages) = BuildPagedButtons(scriptButtons, pageListDto);
        _currentScriptsPage = currentPage;

        await ReplaceOrSendMessage(
            ConstantData.ReplaceText(
                $"Скрипты\r\nСтраница {currentPage + 1} из {totalPages}",
                _userData),
            callbackQuery?.Message,
            chat,
            inlineKeyboard,
            ct);
    }

    private async Task StartUpdateScriptScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var scriptDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (scriptDto.ObjectID == null)
            return;

        var updateScriptContext = new ScenarioContext(ScenarioType.UpdateScript);
        updateScriptContext.Data["ScriptId"] = scriptDto.ObjectID.Value;
        var updateScriptScenario = new UpdateScriptScenario(_scriptsService, _botUsersService);
        _scenarios = _scenarios.Append(updateScriptScenario).ToList();
        await ProcessScenarioAsync(updateScriptContext, update, ct);
    }

    private async Task StartDeleteScriptScenarioAsync(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var scriptDto = CallbackDtoIdObject.FromString(callbackQuery.Data!);
        if (scriptDto.ObjectID == null)
            return;

        var deleteScriptContext = new ScenarioContext(ScenarioType.DeleteScript);
        deleteScriptContext.Data["ScriptId"] = scriptDto.ObjectID.Value;
        var deleteScriptScenario = new DeleteScriptScenario(_scriptsService, _jobRunsService, _serversService, _botUsersService);
        _scenarios = _scenarios.Append(deleteScriptScenario).ToList();
        await ProcessScenarioAsync(deleteScriptContext, update, ct);
    }

    private static string BuildScriptDetailText(Script script)
    {
        var description = script.Description ?? string.Empty;

        return $"Имя: {script.Name}\r\n" +
               $"Описание: {description}\r\n" +
               $"JSON ответ: {(script.ReturnData ? "да" : "нет")}\r\n" +
               $"Таймаут: {script.TimeoutSeconds} сек";
    }
}
