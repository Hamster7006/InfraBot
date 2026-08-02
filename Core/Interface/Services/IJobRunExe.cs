using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

/// <summary>
/// Исполнитель JobRun: очередь и запуск через WinRM в фоне.
/// </summary>
public interface IJobRunExe
{
    /// <summary>Ставит задачу в очередь на выполнение (не блокирует вызывающий поток).</summary>
    Task EnqueueAsync(JobRun job, CancellationToken ct);
}
