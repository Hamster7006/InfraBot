using InfraBot.Core.DataAccess.Models;
using InfraBot.Entities;

namespace InfraBot.Infrastructure.DataAccess;

internal static class ModelMapper
{
    public static BotUser MapFromModel(BotUserModel model) =>
        new()
        {
            Id = model.Id,
            TelegramId = model.TelegramId,
            Username = model.Username,
            Status = model.Status,
            Pending = model.Pending,
            CreatedAt = model.CreatedAt
        };

    public static BotUserModel MapToModel(BotUser entity) =>
        new()
        {
            Id = entity.Id,
            TelegramId = entity.TelegramId,
            Username = entity.Username,
            Status = entity.Status,
            Pending = entity.Pending,
            CreatedAt = entity.CreatedAt
        };

    public static SvcSamAccount MapFromModel(SvcSamAccountModel model) =>
        new()
        {
            Id = model.Id,
            SamAccountName = model.SamAccountName,
            Password = model.Password
        };

    public static SvcSamAccountModel MapToModel(SvcSamAccount entity) =>
        new()
        {
            Id = entity.Id,
            SamAccountName = entity.SamAccountName,
            Password = entity.Password
        };

    public static Script MapFromModel(ScriptModel model) =>
        new()
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            Content = model.Content,
            ReturnData = model.ReturnData,
            TimeoutSeconds = model.TimeoutSeconds,
            CreatedAt = model.CreatedAt,
            CreatedById = model.CreatedById
        };

    public static ScriptModel MapToModel(Script entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Content = entity.Content,
            ReturnData = entity.ReturnData,
            TimeoutSeconds = entity.TimeoutSeconds,
            CreatedAt = entity.CreatedAt,
            CreatedById = entity.CreatedById
        };

    public static Server MapFromModel(
        ServerModel model,
        IEnumerable<Guid> scriptRequirements,
        IEnumerable<Guid> grantedUserIds) =>
        new()
        {
            Id = model.Id,
            ServerName = model.ServerName,
            IpAddress = model.IpAddress,
            Description = model.Description,
            RegisteredByUserId = model.RegisteredByUserId,
            WinRmPort = model.WinRmPort,
            SvcSamAccountId = model.SvcSamAccountId,
            ScriptRequirements = scriptRequirements.ToList(),
            GrantedUserIds = grantedUserIds.ToList()
        };

    public static ServerModel MapToModel(Server entity) =>
        new()
        {
            Id = entity.Id,
            ServerName = entity.ServerName,
            IpAddress = entity.IpAddress,
            Description = entity.Description,
            RegisteredByUserId = entity.RegisteredByUserId,
            WinRmPort = entity.WinRmPort,
            SvcSamAccountId = entity.SvcSamAccountId
        };

    public static JobRun MapFromModel(JobRunModel model) =>
        new()
        {
            Id = model.Id,
            Status = model.Status,
            ResultJson = model.ResultJson,
            ErrorMessage = model.ErrorMessage,
            ExitCode = model.ExitCode,
            CreatedAt = model.CreatedAt,
            StartedAt = model.StartedAt,
            FinishedAt = model.FinishedAt,
            ScriptId = model.ScriptId,
            ServerId = model.ServerId,
            InitiatedById = model.InitiatedById,
            ChatId = model.ChatId
        };

    public static JobRunModel MapToModel(JobRun entity) =>
        new()
        {
            Id = entity.Id,
            Status = entity.Status,
            ResultJson = entity.ResultJson,
            ErrorMessage = entity.ErrorMessage,
            ExitCode = entity.ExitCode,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            FinishedAt = entity.FinishedAt,
            ScriptId = entity.ScriptId,
            ServerId = entity.ServerId,
            InitiatedById = entity.InitiatedById,
            ChatId = entity.ChatId
        };
}
