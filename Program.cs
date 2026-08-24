internal class Program
{
    private static async Task Main(string[] args)
    {
        var bot = new InfraBot.TelegramBot.TelegramBotInit();
        await bot.StartTelegramBotInitAsync();
    }
}
