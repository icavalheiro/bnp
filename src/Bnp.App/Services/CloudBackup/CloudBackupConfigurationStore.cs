using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bnp.Services.CloudBackup;

internal sealed class CloudBackupConfigurationStore
{
    private static readonly byte[] Entropy = "BNP.CloudBackup.v1"u8.ToArray();
    private readonly string _configurationPath;

    public CloudBackupConfigurationStore(string configurationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);
        _configurationPath = configurationPath;
    }

    public CloudBackupSettings? Load()
    {
        if (!File.Exists(_configurationPath))
        {
            return null;
        }

        using var stream = File.OpenRead(_configurationPath);
        var stored = JsonSerializer.Deserialize(
            stream,
            CloudBackupJsonContext.Default.StoredCloudBackupSettings);
        if (stored is null)
        {
            return null;
        }

        var refreshToken = LoadRefreshToken(stored.ProtectedRefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        return new CloudBackupSettings(
            refreshToken);
    }

    public void Save(CloudBackupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(_configurationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var protectedRefreshToken = SaveRefreshToken(settings.RefreshToken);
        var stored = new StoredCloudBackupSettings(
            protectedRefreshToken);
        var temporaryPath = $"{_configurationPath}.tmp";
        using (var stream = File.Create(temporaryPath))
        {
            JsonSerializer.Serialize(
                stream,
                stored,
                CloudBackupJsonContext.Default.StoredCloudBackupSettings);
        }

        File.Move(temporaryPath, _configurationPath, overwrite: true);
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(
                _configurationPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public void Clear()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                RunSecretTool(["clear", "application", "bnp", "purpose", "cloud-backup"]);
            }
        }
        finally
        {
            File.Delete(_configurationPath);
        }
    }

    private static string? LoadRefreshToken(string? protectedRefreshToken)
    {
        if (OperatingSystem.IsWindows())
        {
            if (string.IsNullOrWhiteSpace(protectedRefreshToken))
            {
                return null;
            }

            var protectedToken = Convert.FromBase64String(protectedRefreshToken);
            var token = ProtectedData.Unprotect(
                protectedToken,
                Entropy,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(token);
        }

        if (OperatingSystem.IsLinux())
        {
            var result = RunSecretTool(
                ["lookup", "application", "bnp", "purpose", "cloud-backup"]);
            return result.ExitCode == 0 ? result.StandardOutput.TrimEnd('\r', '\n') : null;
        }

        throw new PlatformNotSupportedException(
            "Cloud credentials are supported on Windows and Linux.");
    }

    private static string? SaveRefreshToken(string refreshToken)
    {
        if (OperatingSystem.IsWindows())
        {
            var token = Encoding.UTF8.GetBytes(refreshToken);
            var protectedToken = ProtectedData.Protect(
                token,
                Entropy,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedToken);
        }

        if (OperatingSystem.IsLinux())
        {
            var result = RunSecretTool(
                ["store", "--label=BNP", "application", "bnp", "purpose", "cloud-backup"],
                refreshToken);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Linux Secret Service rejected the credential ({result.StandardError.Trim()}).");
            }

            return null;
        }

        throw new PlatformNotSupportedException(
            "Cloud credentials are supported on Windows and Linux.");
    }

    private static SecretToolResult RunSecretTool(
        IReadOnlyList<string> arguments,
        string? standardInput = null)
    {
        var startInfo = new ProcessStartInfo("secret-tool")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Linux Secret Service could not be started.");
            if (standardInput is not null)
            {
                process.StandardInput.Write(standardInput);
                process.StandardInput.Close();
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new SecretToolResult(process.ExitCode, output, error);
        }
        catch (Win32Exception exception)
        {
            throw new PlatformNotSupportedException(
                "Linux cloud credentials require secret-tool and an active Secret Service.",
                exception);
        }
    }
}

internal sealed record StoredCloudBackupSettings(
    string? ProtectedRefreshToken);

internal sealed record SecretToolResult(int ExitCode, string StandardOutput, string StandardError);

[JsonSerializable(typeof(StoredCloudBackupSettings))]
internal sealed partial class CloudBackupJsonContext : JsonSerializerContext;