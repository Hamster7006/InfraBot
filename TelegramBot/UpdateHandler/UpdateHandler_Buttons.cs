using InfraBot.Helpers;
using InfraBot.Infrastructure.Callback;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    private async Task ReplaceOrSendMessage(string text, Message? message, Chat chat, ReplyKeyboardMarkup? replyKeyboardMarkup, CancellationToken ct)
    {
        if (message != null)
            await _telegramBotClient.EditMessageText(chat, message.MessageId, text, cancellationToken: ct);
        else
            await _telegramBotClient.SendMessage(chat, text, replyMarkup: replyKeyboardMarkup, cancellationToken: ct);
    }

    private async Task ReplaceOrSendMessage(string text, Message? message, Chat chat, InlineKeyboardMarkup inlineKeyboard, CancellationToken ct)
    {
        if (message != null)
            await _telegramBotClient.EditMessageText(chat, message.MessageId, text, replyMarkup: inlineKeyboard, cancellationToken: ct);
        else
            await _telegramBotClient.SendMessage(chat, text, replyMarkup: inlineKeyboard, cancellationToken: ct);
    }

    private (InlineKeyboardMarkup Keyboard, int CurrentPage, int TotalPages) BuildPagedButtons(
        IReadOnlyList<KeyValuePair<string, string>> callbackData,
        PagedListCallbackDto pageListDto)
    {
        var totalPages = Math.Max(1, (callbackData.Count + _pageSize - 1) / _pageSize);
        var currentPage = Math.Clamp(pageListDto.Page, 0, totalPages - 1);
        var inlineKeyboardMarkup = new InlineKeyboardMarkup();

        var itemsOnPage = callbackData.GetBatchByNumber(_pageSize, currentPage)?
            .Cast<KeyValuePair<string, string>>()
            .ToList() ?? [];

        for (var i = 0; i < itemsOnPage.Count; i += _columns)
        {
            var row = new List<InlineKeyboardButton>();
            for (var j = i; j < Math.Min(i + _columns, itemsOnPage.Count); j++)
            {
                var item = itemsOnPage[j];
                row.Add(InlineKeyboardButton.WithCallbackData(item.Key, item.Value));
            }

            inlineKeyboardMarkup.AddNewRow(row.ToArray());
        }

        AddNavigationButtons(inlineKeyboardMarkup, pageListDto, currentPage, totalPages);

        return (inlineKeyboardMarkup, currentPage, totalPages);
    }

    private void AddNavigationButtons(
        InlineKeyboardMarkup keyboard,
        PagedListCallbackDto pageListDto,
        int currentPage,
        int totalPages)
    {
        if (totalPages <= 1)
            return;

        var backButtons1 = AddPageButton(pageListDto, currentPage, totalPages, -1, "⬅️");
        var nextButtons1 = AddPageButton(pageListDto, currentPage, totalPages, 1, "➡️");

        var backButtons10 = AddPageButton(pageListDto, currentPage, totalPages, -10, "◀ -10");
        var nextButtons10 = AddPageButton(pageListDto, currentPage, totalPages, 10, "+10 ▶");

        var backButtons100 = AddPageButton(pageListDto, currentPage, totalPages, -100, "-100 ⏩");
        var nextButtons100 = AddPageButton(pageListDto, currentPage, totalPages, 100, "⏪ +100");

        var navButtons = new List<InlineKeyboardButton?> { backButtons10, backButtons1, nextButtons1, nextButtons10 }
            .Where(x => x != null)
            .Cast<InlineKeyboardButton>()
            .ToArray();
        if (navButtons.Length > 0)
            keyboard.AddNewRow(navButtons);

        var navButtons2 = new List<InlineKeyboardButton?> { backButtons100, nextButtons100 }
            .Where(x => x != null)
            .Cast<InlineKeyboardButton>()
            .ToArray();
        if (navButtons2.Length > 0)
            keyboard.AddNewRow(navButtons2);
    }

    private InlineKeyboardButton? AddPageButton(
        PagedListCallbackDto pageListDto,
        int currentPage,
        int totalPages,
        int pageDelta,
        string text)
    {
        var targetPage = currentPage + pageDelta;
        if (targetPage < 0 || targetPage >= totalPages)
            return null;

        var dto = new PagedListCallbackDto
        {
            Action = pageListDto.Action,
            ObjectID = pageListDto.ObjectID,
            Page = targetPage
        };
        return InlineKeyboardButton.WithCallbackData(text, dto.ToString());
    }
    private InlineKeyboardButton CreateButtonInline(string text, string callbackData, KeyboardButtonStyle style)
    {
        var button = InlineKeyboardButton.WithCallbackData(text, callbackData);
        button.Style = style;

        return button;
    }
    private InlineKeyboardButton CreateButtonInline(string text, string callbackData)
    {
        return InlineKeyboardButton.WithCallbackData(text, callbackData);
    }
}
