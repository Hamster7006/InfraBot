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
    internal const string UserControl = "/usercontrol";
    

    internal static readonly Dictionary<string, CommndsData> CommandsDictionary = new()
    {
        [Start] = new CommndsData
        {
            Description = "Регистрация и авторизация",
            Levels = [UserStatus.Guest, UserStatus.Operator, UserStatus.MainOperator, UserStatus.Admin]
        },
        [Pending] = new CommndsData
        {
            Description = "Запрос повышения роли (Guest → Operator, Operator → MainOperator)",
            Levels = [UserStatus.Guest, UserStatus.Operator]
        },
        [ListServers] = new CommndsData
        {
            Description = "Список доступных серверов",
            Levels = [UserStatus.Operator, UserStatus.MainOperator, UserStatus.Admin]
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
        [UserControl] = new CommndsData
        {
            Description = "Управление пользователями",
            Levels = [UserStatus.Admin]
        },
    };

    
}


