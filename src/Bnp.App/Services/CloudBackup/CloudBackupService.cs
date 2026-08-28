using Bnp.Persistence;
using System.Security.Cryptography;
using System.Text.Json;

namespace Bnp.Services.CloudBackup;

internal sealed class CloudBackupService : IDisposable
{
    private const string DefaultDropboxClientId = "86e6im2xh5nhqqj";

    private readonly SqliteDocumentRepository _repository;
    private readonly CloudBackupConfigurationStore _configurationStore;
    private readonly HttpClient _httpClient = new();
    private readonly OAuthLoopbackClient _oauthClient;
    private bool _isDisposed;

    public CloudBackupService(SqliteDocumentRepository repository, string configurationPath)
    {
        _repository = repository;
        _configurationStore = new CloudBackupConfigurationStore(configurationPath);
        _oauthClient = new OAuthLoopbackClient(_httpClient);
    }

    public CloudBackupConnectionState GetConnectionState()
    {
        try
        {
            var settings = _configurationStore.Load();
            return new CloudBackupConnectionState(settings is not null);
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException or FormatException or IOException or
                PlatformNotSupportedException or InvalidOperationException)
        {
            return new CloudBackupConnectionState(false);
        }
    }

    public async Task<bool> ConnectAsync(
        Action? beforeMerge,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var provider = GetProviderDefinition();
        var token = await _oauthClient.AuthorizeAsync(provider, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new InvalidOperationException("The cloud provider did not issue an offline refresh token.");
        }

        var settings = new CloudBackupSettings(token.RefreshToken);
        _configurationStore.Save(settings);
        return await SynchronizeAsync(settings, token.AccessToken, beforeMerge, cancellationToken);
    }

    public async Task<bool> SynchronizeAsync(
        Action? beforeMerge,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var settings = _configurationStore.Load();
        if (settings is null)
        {
            return false;
        }

        var provider = GetProviderDefinition();
        var token = await _oauthClient.RefreshAsync(provider, settings.RefreshToken, cancellationToken);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken) &&
            !string.Equals(token.RefreshToken, settings.RefreshToken, StringComparison.Ordinal))
        {
            settings = settings with { RefreshToken = token.RefreshToken };
            _configurationStore.Save(settings);
        }

        return await SynchronizeAsync(settings, token.AccessToken, beforeMerge, cancellationToken);
    }

    public void Disconnect()
    {
        _configurationStore.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _httpClient.Dispose();
        _isDisposed = true;
    }

    private async Task<bool> SynchronizeAsync(
        CloudBackupSettings settings,
        string accessToken,
        Action? beforeMerge,
        CancellationToken cancellationToken)
    {
        const string remoteFileName = "bnp.db";
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "bnp-backups",
            Guid.NewGuid().ToString("N"));
        var cloudPath = Path.Combine(temporaryDirectory, "cloud.db");
        var mergedPath = Path.Combine(temporaryDirectory, "merged.db");
        try
        {
            Directory.CreateDirectory(temporaryDirectory);
            var provider = new DropboxBackupProvider(_httpClient);
            var hasCloudDatabase = await provider.DownloadAsync(
                accessToken,
                remoteFileName,
                cloudPath,
                cancellationToken);
            var databaseChanged = false;
            if (hasCloudDatabase)
            {
                beforeMerge?.Invoke();
                databaseChanged = _repository.MergeFrom(cloudPath);
            }

            _repository.CreateBackup(mergedPath);
            await provider.UploadAsync(
                accessToken,
                mergedPath,
                remoteFileName,
                cancellationToken);
            return databaseChanged;
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    private static OAuthProviderDefinition GetProviderDefinition()
    {
        var clientId = Environment.GetEnvironmentVariable("BNP_DROPBOX_CLIENT_ID")
            ?? DefaultDropboxClientId;
        return new OAuthProviderDefinition(
            clientId,
            "https://www.dropbox.com/oauth2/authorize",
            "https://api.dropboxapi.com/oauth2/token",
            string.Empty,
            new Dictionary<string, string> { ["token_access_type"] = "offline" });
    }
}