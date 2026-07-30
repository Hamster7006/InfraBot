using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Infrastracture.Services;

internal static class AccessRules
{
    internal static bool HasServerAccess(BotUser user, Server server)
    {
        if (user.Status == UserStatus.Admin)
            return true;

        return server.GrantedUserIds.Contains(user.Id);
    }

    internal static bool CanRunScript(BotUser user, Server server, Script script)
    {
        if (user.Status is UserStatus.Blocked or UserStatus.Guest)
            return false;

        if (!server.IsEnabled || !script.IsEnabled)
            return false;

        if (user.Status == UserStatus.Admin)
            return server.ScriptRequirements.ContainsKey(script.Id);

        if (!HasServerAccess(user, server))
            return false;

        if (!server.ScriptRequirements.TryGetValue(script.Id, out var requiredRole))
            return false;

        return (int)user.Status >= (int)requiredRole;
    }
}
