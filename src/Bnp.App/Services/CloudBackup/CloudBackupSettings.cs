namespace Bnp.Services.CloudBackup;

internal sealed record CloudBackupSettings(
    string RefreshToken)
{
    public const string RemoteFolderPath = "/bnp/";
}

internal sealed record CloudBackupConnectionState(bool IsConnected);