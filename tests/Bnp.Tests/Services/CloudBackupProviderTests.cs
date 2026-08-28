using System.Net;
using System.Text;
using Bnp.Services.CloudBackup;

namespace Bnp.Tests.Services;

public sealed class CloudBackupProviderTests
{
    [Fact]
    public async Task DropboxDownloadsAndOverwritesStableDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string? uploadArguments = null;
        var handler = new StubHttpMessageHandler(async (request, requestCancellationToken) =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/download", StringComparison.Ordinal) == true)
            {
                return Response(HttpStatusCode.OK, "cloud-db");
            }

            if (request.RequestUri?.AbsolutePath.EndsWith("/upload", StringComparison.Ordinal) == true)
            {
                uploadArguments = request.Headers.GetValues("Dropbox-API-Arg").Single();
                _ = await request.Content!.ReadAsByteArrayAsync(requestCancellationToken);
                return Response(HttpStatusCode.OK, "{}");
            }

            return Response(HttpStatusCode.Conflict, "{}");
        });
        using var httpClient = new HttpClient(handler);
        var provider = new DropboxBackupProvider(httpClient);
        var testDirectory = CreateTestDirectory();
        var downloadPath = Path.Combine(testDirectory, "download.db");
        var uploadPath = Path.Combine(testDirectory, "upload.db");

        try
        {
            await File.WriteAllTextAsync(uploadPath, "local-db", cancellationToken);

            var found = await provider.DownloadAsync(
                "token", "bnp.db", downloadPath, cancellationToken);
            await provider.UploadAsync(
                "token", uploadPath, "bnp.db", cancellationToken);

            Assert.True(found);
            Assert.Equal("cloud-db", await File.ReadAllTextAsync(downloadPath, cancellationToken));
            Assert.Contains("/bnp/bnp.db", uploadArguments, StringComparison.Ordinal);
            Assert.Contains("\"mode\":\"overwrite\"", uploadArguments, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bnp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request, cancellationToken);
        }
    }
}