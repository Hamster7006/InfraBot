using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.Helpers;
using Telegram.Bot;

namespace InfraBot.Infrastructure.Services;

/// <summary>
/// Исполнитель JobRun через WinRM. Задачи обрабатываются фоновым worker по одной.
/// </summary>
internal sealed class JobRunExe : IJobRunExe
{
    private readonly IJobRunRepository _jobRuns;
    private readonly IServerRepository _servers;
    private readonly IScriptRepository _scripts;
    private readonly ISvcSamAccountRepository _svcAccounts;
    private readonly ITelegramBotClient _bot;
    private readonly Channel<Guid> _queue;
    private readonly Task _workerTask;

    public JobRunExe(
        IJobRunRepository jobRuns,
        IServerRepository servers,
        IScriptRepository scripts,
        ISvcSamAccountRepository svcAccounts,
        ITelegramBotClient bot,
        CancellationToken appStopping)
    {
        _jobRuns = jobRuns;
        _servers = servers;
        _scripts = scripts;
        _svcAccounts = svcAccounts;
        _bot = bot;
        _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _workerTask = Task.Run(() => ProcessQueueAsync(appStopping));
    }

    public async Task EnqueueAsync(JobRun job, CancellationToken ct)
    {
        await _queue.Writer.WriteAsync(job.Id, ct);
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var jobId in _queue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    var job = await _jobRuns.GetAsync(jobId, ct);
                    if (job == null)
                    {
                        Console.WriteLine($"Задача {jobId} не найдена в очереди.");
                        continue;
                    }

                    await ExecuteJobAsync(job, CancellationToken.None);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Console.WriteLine($"Ошибка обработки задачи {jobId}: {exception.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task ExecuteJobAsync(JobRun job, CancellationToken ct)
    {
        job.Status = JobRunStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _jobRuns.UpdateAsync(job, ct);

        try
        {
            var server = await _servers.GetAsync(job.ServerId, ct);
            if (server == null)
                throw new InvalidOperationException("Сервер не найден.");

            var script = await _scripts.GetAsync(job.ScriptId, ct);
            if (script == null)
                throw new InvalidOperationException("Скрипт не найден.");

            if (server.SvcSamAccountId == Guid.Empty)
                throw new InvalidOperationException("У сервера не задана WinRM учётная запись.");

            var svcAccount = await _svcAccounts.GetAsync(server.SvcSamAccountId, ct);
            if (svcAccount == null)
                throw new InvalidOperationException("WinRM учётная запись не найдена.");

            var targetHost = server.IpAddress;
            if (string.IsNullOrWhiteSpace(targetHost))
                throw new InvalidOperationException("У сервера не задан IP-адрес.");

            var output = await RunScriptOnServer(
                targetHost,
                server.WinRmPort,
                svcAccount.SamAccountName,
                svcAccount.Password,
                script.Content,
                script.TimeoutSeconds,
                ct);

            job.Status = JobRunStatus.Success;
            job.ExitCode = 0;
            job.ResultJson = script.ReturnData ? output : null;
            job.ErrorMessage = null;
        }
        catch (Exception exception)
        {
            job.Status = JobRunStatus.Failed;
            job.ExitCode = 1;
            job.ErrorMessage = exception.Message;
        }

        job.FinishedAt = DateTime.UtcNow;
        await _jobRuns.UpdateAsync(job, ct);
        await SendCompletionMessageAsync(job, ct);
    }

    private async Task SendCompletionMessageAsync(JobRun job, CancellationToken ct)
    {
        if (job.ChatId == 0)
            return;

        try
        {
            var text = JobRunResultFormatter.BuildCompletionMessage(
                job.Id,
                job.Status,
                job.ResultJson,
                job.ErrorMessage);

            await _bot.SendMessage(job.ChatId, text, cancellationToken: ct);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Не удалось отправить результат задачи {job.Id}: {exception.Message}");
        }
    }

    private static async Task<string> RunScriptOnServer(
        string targetHost,
        int winRmPort,
        string userName,
        string password,
        string scriptContent,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var useSsl = winRmPort == 5986;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"infrabot-job-{Guid.NewGuid():N}.ps1");

        var powerShellScript = new StringBuilder();
        powerShellScript.AppendLine("$ErrorActionPreference = 'Stop'");
        powerShellScript.AppendLine($"$securePassword = ConvertTo-SecureString '{EscapeForPowerShellSingleQuoted(password)}' -AsPlainText -Force");
        powerShellScript.AppendLine($"$credential = New-Object System.Management.Automation.PSCredential('{EscapeForPowerShellSingleQuoted(userName)}', $securePassword)");
        powerShellScript.AppendLine("$sessionOption = New-PSSessionOption -SkipCACheck -SkipCNCheck");
        powerShellScript.AppendLine($"$session = New-PSSession -ComputerName '{EscapeForPowerShellSingleQuoted(targetHost)}' -Port {winRmPort} -UseSSL:${useSsl.ToString().ToLowerInvariant()} -Credential $credential -SessionOption $sessionOption");
        powerShellScript.AppendLine("try {");
        powerShellScript.AppendLine("    $result = Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create(@'");
        powerShellScript.AppendLine(scriptContent.Replace("'", "''"));
        powerShellScript.AppendLine("'@))");
        powerShellScript.AppendLine("    if ($null -eq $result) { Write-Output '' }");
        powerShellScript.AppendLine("    elseif ($result -is [string]) { Write-Output $result }");
        powerShellScript.AppendLine("    else { $result | ConvertTo-Json -Compress -Depth 5 }");
        powerShellScript.AppendLine("}");
        powerShellScript.AppendLine("finally {");
        powerShellScript.AppendLine("    if ($null -ne $session) { Remove-PSSession $session }");
        powerShellScript.AppendLine("}");

        await File.WriteAllTextAsync(scriptPath, powerShellScript.ToString(), ct);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var readOutput = process.StandardOutput.ReadToEndAsync(ct);
            var readError = process.StandardError.ReadToEndAsync(ct);
            var timeout = TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 30));

            var finished = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), ct);
            if (!finished)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"Скрипт не завершился за {timeoutSeconds} сек.");
            }

            var output = await readOutput;
            var error = await readError;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);

            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException(error);

            return output.Trim();
        }
        finally
        {
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
    }

    private static string EscapeForPowerShellSingleQuoted(string value)
        => value.Replace("'", "''");
}
