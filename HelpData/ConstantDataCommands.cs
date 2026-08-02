using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.HelpData;

internal partial class ConstantData
{
    internal const string Start = "/start";
    internal const string Pending = "/pending";
    internal const string ListServers = "/listservers";
    internal const string ListScripts = "/scripts";
    internal const string PendingRequests = "/pendingrequests";
    internal const string AddServer = "/addserver";
    internal const string AddScript = "/addscript";
    internal const string ListSvcAccounts = "/svcaccounts";
    internal const string AddSvcAccount = "/addsvcaccount";
    internal const string UserControl = "/usercontrol";
    internal const string AdminControl = "/admincontrol";
    internal const string Cancel = "/cancel"; 
    internal const string Help = "/help";
    internal const string Info = "/info";
    internal const string About = "/about";
    internal const string Report = "/report";
    internal const string ReportAll = "/reportall";

    internal static readonly Dictionary<string, CommndsData> CommandsDictionary = new()
    {
        [Start] = new CommndsData
        {
            Description = "Регистрация и авторизация",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.Admin]
        },
        [Pending] = new CommndsData
        {
            Description = "Запрос повышения роли (Guest → Operator)",
            Levels = [UserStatus.Guest]
        },
        [ListServers] = new CommndsData
        {
            Description = "Список доступных серверов",
            Levels = [UserStatus.Operator, UserStatus.Admin]
        },
        [ListScripts] = new CommndsData
        {
            Description = "Список скриптов",
            Levels = [UserStatus.Admin]
        },
        [PendingRequests] = new CommndsData
        {
            Description = "Заявки на повышение роли",
            Levels = [UserStatus.Admin]
        },
        [AddServer] = new CommndsData
        {
            Description = "Добавление сервера",
            Levels = [UserStatus.Admin]
        },
        [AddScript] = new CommndsData
        {
            Description = "Добавление скрипта",
            Levels = [UserStatus.Admin]
        },
        [ListSvcAccounts] = new CommndsData
        {
            Description = "Список WinRM учётных записей",
            Levels = [UserStatus.Admin]
        },
        [AddSvcAccount] = new CommndsData
        {
            Description = "Добавление WinRM учётной записи",
            Levels = [UserStatus.Admin]
        },
        [UserControl] = new CommndsData
        {
            Description = "Управление пользователями",
            Levels = [UserStatus.Admin]
        },
        [AdminControl] = new CommndsData
        {
            Description = "Модуль администрирования",
            Levels = [UserStatus.Admin]
        },
        [Cancel] = new CommndsData
        {
            Description = "Отмена текущего сценария",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.Admin]
        },
        [Help] = new CommndsData
        {
            Description = "Список доступных команд",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.Admin]
        },
        [Info] = new CommndsData
        {
            Description = "Информация о вашем профиле",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.Admin]
        },
        [About] = new CommndsData
        {
            Description = "О боте",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.Admin]
        },
        [Report] = new CommndsData
        {
            Description = "Отчёт по вашим запускам (7 дней)",
            Levels = [UserStatus.Operator, UserStatus.Admin]
        },
        [ReportAll] = new CommndsData
        {
            Description = "Отчёт по всем запускам (7 дней)",
            Levels = [UserStatus.Admin]
        },
    };
}
