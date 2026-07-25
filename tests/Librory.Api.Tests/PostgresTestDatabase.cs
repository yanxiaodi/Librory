using System.Diagnostics;
using Npgsql;

namespace Librory.Api.Tests;

internal sealed class PostgresTestDatabase : IAsyncDisposable
{
    private const string Image = "postgres:16-alpine";
    private const string Username = "postgres";
    private const string Password = "postgres";
    private const string AdminDatabase = "postgres";
    private static readonly Lazy<Task<PostgresTestHost>> Host = new(InitializeHostAsync);

    private readonly string _databaseName;
    private readonly PostgresTestHost _host;

    private PostgresTestDatabase(PostgresTestHost host, string databaseName)
    {
        _host = host;
        _databaseName = databaseName;
        ConnectionString = $"Host=127.0.0.1;Port={host.Port};Database={databaseName};Username={Username};Password={Password};Pooling=false";
    }

    public string ConnectionString { get; }

    public static async Task<PostgresTestDatabase> CreateAsync()
    {
        var host = await Host.Value;
        var databaseName = $"librory_test_{Guid.NewGuid():N}";

        await host.CreateDatabaseAsync(databaseName);

        return new PostgresTestDatabase(host, databaseName);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DropDatabaseAsync(_databaseName);
    }

    private static async Task<PostgresTestHost> InitializeHostAsync()
    {
        var containerName = $"librory-postgres-tests-{Environment.ProcessId}";

        await TryRemoveContainerAsync(containerName);
        await RunDockerAsync(
            "run",
            "--name",
            containerName,
            "--rm",
            "-e",
            $"POSTGRES_USER={Username}",
            "-e",
            $"POSTGRES_PASSWORD={Password}",
            "-e",
            $"POSTGRES_DB={AdminDatabase}",
            "-p",
            "127.0.0.1::5432",
            "-d",
            Image);

        var port = await GetMappedPortAsync(containerName);
        var host = new PostgresTestHost(containerName, port);
        await host.WaitUntilReadyAsync();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                host.StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore shutdown cleanup failures.
            }
        };

        return host;
    }

    private static async Task TryRemoveContainerAsync(string containerName)
    {
        try
        {
            await RunDockerAsync("rm", "-f", containerName);
        }
        catch
        {
            // Ignore stale-container cleanup failures.
        }
    }

    private static async Task<int> GetMappedPortAsync(string containerName)
    {
        var output = await RunDockerAsync("port", containerName, "5432/tcp");
        var endpoints = output.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        var endpoint = endpoints.FirstOrDefault(line => line.StartsWith("127.0.0.1:", StringComparison.Ordinal));

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                $"docker port {containerName} 5432/tcp did not return a usable 127.0.0.1 mapping. Output: {output}");
        }

        var address = endpoint[(endpoint.LastIndexOf(':') + 1)..];
        return int.Parse(address, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<string> RunDockerAsync(params string[] args)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in args)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        using var process = StartDockerProcess(processStartInfo);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var waitForExitTask = process.WaitForExitAsync(timeoutCts.Token);

        try
        {
            await waitForExitTask;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            TryKillProcess(process);
            throw new TimeoutException($"docker {string.Join(" ", args)} timed out after 30 seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker {string.Join(" ", args)} failed with exit code {process.ExitCode}: {standardError}{Environment.NewLine}{standardOutput}");
        }

        return standardOutput.Trim();
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static Process StartDockerProcess(ProcessStartInfo processStartInfo)
    {
        try
        {
            return Process.Start(processStartInfo)
                ?? throw new InvalidOperationException("Failed to start docker.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException("Docker is required to run the API integration tests. Ensure docker is installed and available on PATH.", exception);
        }
    }

    private sealed class PostgresTestHost
    {
        private readonly string _containerName;
        private readonly string _adminConnectionString;

        public PostgresTestHost(string containerName, int port)
        {
            _containerName = containerName;
            Port = port;
            _adminConnectionString = $"Host=127.0.0.1;Port={port};Database={AdminDatabase};Username={Username};Password={Password};Pooling=false";
        }

        public int Port { get; }

        public async Task WaitUntilReadyAsync()
        {
            var lastException = default(Exception);

            for (var attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(_adminConnectionString);
                    await connection.OpenAsync();
                    await using var command = connection.CreateCommand();
                    command.CommandText = "select 1;";
                    _ = await command.ExecuteScalarAsync();
                    return;
                }
                catch (Exception exception) when (attempt < 59)
                {
                    lastException = exception;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            throw new TimeoutException("Timed out waiting for the PostgreSQL test container to become ready.", lastException);
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $@"CREATE DATABASE ""{databaseName}"";";
            await command.ExecuteNonQueryAsync();
        }

        public async Task DropDatabaseAsync(string databaseName)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_adminConnectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = $@"DROP DATABASE IF EXISTS ""{databaseName}"" WITH (FORCE);";
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        public async Task StopAsync()
        {
            await RunDockerAsync("stop", _containerName);
        }
    }
}
