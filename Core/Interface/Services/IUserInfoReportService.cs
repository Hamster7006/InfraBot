using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

public interface IUserInfoReportService
{
    Task<UserInfoReport> BuildAsync(BotUser user, CancellationToken ct);
}
