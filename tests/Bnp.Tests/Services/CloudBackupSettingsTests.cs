using Bnp.Services.CloudBackup;

namespace Bnp.Tests.Services;

public sealed class CloudBackupSettingsTests
{
    [Fact]
    public void RemoteFolderPathIsFixed()
    {
        Assert.Equal("/bnp/", CloudBackupSettings.RemoteFolderPath);
    }

    [Fact]
    public void ConfigurationStoreProtectsRefreshTokenForCurrentUser()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var testDirectory = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        var configurationPath = Path.Combine(testDirectory, "cloud-backup.json");
        const string refreshToken = "test-refresh-token-that-must-not-be-plain-text";

        try
        {
            var store = new CloudBackupConfigurationStore(configurationPath);
            var settings = new CloudBackupSettings(refreshToken);

            store.Save(settings);

            Assert.DoesNotContain(refreshToken, File.ReadAllText(configurationPath), StringComparison.Ordinal);
            Assert.Equal(settings, store.Load());
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}