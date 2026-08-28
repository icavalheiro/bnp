using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bnp.Services.CloudBackup;

internal sealed class OAuthLoopbackClient(HttpClient httpClient)
{
    private const string ListenerPrefix = "http://127.0.0.1:53682/";
    private const string RedirectUri = $"{ListenerPrefix}oauth/callback/";

    public async Task<OAuthTokenResponse> AuthorizeAsync(
        OAuthProviderDefinition provider,
        CancellationToken cancellationToken)
    {
        var verifier = CreateRandomValue(64);
        var challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = CreateRandomValue(32);
        using var listener = new HttpListener();
        listener.Prefixes.Add(ListenerPrefix);
        listener.Start();

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256"
        };
        if (!string.IsNullOrWhiteSpace(provider.Scope))
        {
            parameters["scope"] = provider.Scope;
        }

        foreach (var parameter in provider.AuthorizationParameters)
        {
            parameters[parameter.Key] = parameter.Value;
        }

        var authorizationUri = BuildUri(provider.AuthorizationEndpoint, parameters);
        Process.Start(new ProcessStartInfo(authorizationUri) { UseShellExecute = true });

        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().WaitAsync(
                TimeSpan.FromMinutes(5),
                cancellationToken);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException("Cloud sign-in timed out.", exception);
        }

        var query = context.Request.QueryString;
        var returnedState = query["state"];
        var code = query["code"];
        var error = query["error"];
        var isValid = string.Equals(state, returnedState, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(code) &&
            string.IsNullOrWhiteSpace(error);
        await WriteBrowserResponseAsync(context.Response, isValid, cancellationToken);

        if (!isValid)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "Cloud sign-in returned an invalid response."
                    : $"Cloud sign-in was not completed ({error}).");
        }

        using var response = await httpClient.PostAsync(
            provider.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = provider.ClientId,
                ["grant_type"] = "authorization_code",
                ["code"] = code!,
                ["code_verifier"] = verifier,
                ["redirect_uri"] = RedirectUri
            }),
            cancellationToken);
        return await ReadTokenResponseAsync(response, cancellationToken);
    }

    public async Task<OAuthTokenResponse> RefreshAsync(
        OAuthProviderDefinition provider,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            provider.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = provider.ClientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }),
            cancellationToken);
        return await ReadTokenResponseAsync(response, cancellationToken);
    }

    private static async Task<OAuthTokenResponse> ReadTokenResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The cloud provider rejected the token request ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync(
                stream,
                CloudProviderJsonContext.Default.OAuthTokenResponse,
                cancellationToken)
            ?? throw new InvalidOperationException("The cloud provider returned an empty token response.");
    }

    private static async Task WriteBrowserResponseAsync(
        HttpListenerResponse response,
        bool success,
        CancellationToken cancellationToken)
    {
        var message = success
            ? "Sign-in complete. You can close this tab and return to BNP."
            : "Sign-in could not be completed. Return to BNP and try again.";
        var body = Encoding.UTF8.GetBytes($"<!doctype html><title>BNP</title><p>{message}</p>");
        response.StatusCode = success ? 200 : 400;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body, cancellationToken);
        response.Close();
    }

    private static string BuildUri(string baseUri, IReadOnlyDictionary<string, string> parameters)
    {
        var query = string.Join(
            '&',
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{baseUri}?{query}";
    }

    private static string CreateRandomValue(int byteCount)
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

internal sealed record OAuthProviderDefinition(
    string ClientId,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string Scope,
    IReadOnlyDictionary<string, string> AuthorizationParameters);

internal sealed record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);