using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Services;
using InfraBot.Scenarios.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.TelegramBot
{
    internal partial class UpdateHandler : IUpdateHandler
    {
        private readonly int _pageSize = 8;
        private readonly int _columns = 2;
        private int _currentPage;

        ITelegramBotClient _telegramBotClient;
        ReplyKeyboardMarkup _replyKeyboardMarkup;
        BotUser? _userData;

        IBotUserRepository _botUserRepository;
        IServerRepository _serverRepository;
        IScriptRepository _scriptRepository;
        IJobRunRepository _jobRunRepository;
        ISvcSamAccountRepository _svcRepository;

        IBotUserService _botUsersService;
        IServerService _serversService;
        IScriptService _scriptsService;
        IJobRunService _jobRunsService;
        ISvcSamAccountService _svcAccountsService;
        IJobRunExe _jobRunExe;
        IUserInfoReportService _userInfoReportService;

        private readonly Dictionary<long, bool> _adminModuleActive = new();
        private readonly IScenarioContextRepository _scenarioContextRepository;
        private IEnumerable<IScenario> _scenarios;

        public UpdateHandler(
            ITelegramBotClient telegramBotClient,
            int data,
            string? connectionString,
            IEnumerable<IScenario> scenarios,
            IScenarioContextRepository scenarioContextRepository,
            CancellationToken ct)
        {
            _telegramBotClient = telegramBotClient;
            _scenarioContextRepository = scenarioContextRepository;
            _scenarios = scenarios;

            var constantDataGenerateRandom = new ConstantDataGenerateRandom();
            (IBotUserRepository botUserRepository,
                IServerRepository serverRepository,
                IScriptRepository scriptRepository,
                IJobRunRepository jobRunRepository,
                ISvcSamAccountRepository svcRepository) = constantDataGenerateRandom.SwitchMemory(data, connectionString);
            _serverRepository = serverRepository;
            _scriptRepository = scriptRepository;
            _botUserRepository = botUserRepository;
            _jobRunRepository = jobRunRepository;
            _svcRepository = svcRepository;

            _botUsersService = new BotUserService(_botUserRepository);
            _serversService = new ServerService(_serverRepository);
            _scriptsService = new ScriptService(_scriptRepository);
            _jobRunsService = new JobRunService(_jobRunRepository, _serverRepository, _scriptRepository);
            _svcAccountsService = new SvcSamAccountService(_svcRepository);
            _jobRunExe = new JobRunExe(
                _jobRunRepository,
                _serverRepository,
                _scriptRepository,
                _svcRepository,
                _telegramBotClient,
                ct);
            _userInfoReportService = new UserInfoReportService(_serversService, _jobRunsService);
        }

        public Task HandleErrorAsync(
        ITelegramBotClient botUsers,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
        {
            Console.WriteLine($"Telegram error ({source}): {exception.Message}");
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            await (update switch
            {
                { Message: { } message } => OnMessage(update, cancellationToken),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, callbackQuery, cancellationToken),
                _ => OnUnknown(update)
            });
        }
    }
}
