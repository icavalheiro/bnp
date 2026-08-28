using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bnp.Services.CloudBackup;

internal interface ICloudBackupProvider
{
    Task<bool> DownloadAsync(
        string accessToken,
        string remoteFileName,
        string localFilePath,
        CancellationToken cancellationToken);

    Task UploadAsync(
        string accessToken,
        string localFilePath,
        string remoteFileName,
        CancellationToken cancellationToken);
}

internal sealed class DropboxBackupProvider(HttpClient httpClient) : ICloudBackupProvider
{
    public async Task<bool> DownloadAsync(
        string accessToken,
        string remoteFileName,
        string localFilePath,
        CancellationToken cancellationToken)
    {
        var folder = CloudBackupSettings.RemoteFolderPath.TrimEnd('/');
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://content.dropboxapi.com/2/files/download");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(
            "Dropbox-API-Arg",
            JsonSerializer.Serialize(
                new DropboxDownloadRequest($"{folder}/{remoteFileName}"),
                CloudProviderJsonContext.Default.DropboxDownloadRequest));
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        await using var destination = File.Create(localFilePath);
        await response.Content.CopyToAsync(destination, cancellationToken);
        return true;
    }

    public async Task UploadAsync(
        string accessToken,
        string localFilePath,
        string remoteFileName,
        CancellationToken cancellationToken)
    {
        var folder = CloudBackupSettings.RemoteFolderPath.TrimEnd('/');
        await EnsureFolderExistsAsync(accessToken, folder, cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://content.dropboxapi.com/2/files/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(
            "Dropbox-API-Arg",
            JsonSerializer.Serialize(
                new DropboxUploadRequest($"{folder}/{remoteFileName}", "overwrite", false),
                CloudProviderJsonContext.Default.DropboxUploadRequest));
        request.Content = new StreamContent(File.OpenRead(localFilePath));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task EnsureFolderExistsAsync(
        string accessToken,
        string folderPath,
        CancellationToken cancellationToken)
    {
        var currentPath = string.Empty;
        foreach (var segment in folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath += $"/{segment}";
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.dropboxapi.com/2/files/create_folder_v2");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(
                new DropboxCreateFolderRequest(currentPath, false),
                CloudProviderJsonContext.Default.DropboxCreateFolderRequest);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Conflict)
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }
}

internal sealed record DropboxDownloadRequest(
    [property: JsonPropertyName("path")] string Path);

internal sealed record DropboxCreateFolderRequest(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("autorename")] bool Autorename);

internal sealed record DropboxUploadRequest(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("autorename")] bool Autorename);

[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(DropboxDownloadRequest))]
[JsonSerializable(typeof(DropboxCreateFolderRequest))]
[JsonSerializable(typeof(DropboxUploadRequest))]
internal sealed partial class CloudProviderJsonContext : JsonSerializerContext;