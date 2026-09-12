using PersonalDigitalVault.Api.DTOs.Backup;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PersonalDigitalVault.Api.Services;

public sealed class GoogleDriveClient
{
    private const string DriveScope = "https://www.googleapis.com/auth/drive.appdata";
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
    private const string DriveApiBase = "https://www.googleapis.com/drive/v3";
    private const string DriveUploadBase = "https://www.googleapis.com/upload/drive/v3";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GoogleDriveClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["GoogleDrive:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["GoogleDrive:ClientSecret"]) &&
        !string.IsNullOrWhiteSpace(_configuration["GoogleDrive:RedirectUri"]);

    public string CreateAuthorizationUrl(string state)
    {
        EnsureConfigured();

        string scope = $"openid email {DriveScope}";
        var query = new Dictionary<string, string>
        {
            ["client_id"] = GetRequired("GoogleDrive:ClientId"),
            ["redirect_uri"] = GetRequired("GoogleDrive:RedirectUri"),
            ["response_type"] = "code",
            ["scope"] = scope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state
        };

        return AuthorizationEndpoint + "?" + string.Join("&",
            query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
    }

    public async Task<GoogleOAuthTokens> ExchangeAuthorizationCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Google authorization code is required.");
        }

        using HttpClient client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = GetRequired("GoogleDrive:ClientId"),
            ["client_secret"] = GetRequired("GoogleDrive:ClientSecret"),
            ["redirect_uri"] = GetRequired("GoogleDrive:RedirectUri"),
            ["grant_type"] = "authorization_code"
        });

        using HttpResponseMessage response = await client.PostAsync(
            TokenEndpoint,
            content,
            cancellationToken);

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Google authorization failed.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        string accessToken = GetJsonString(document.RootElement, "access_token")
            ?? throw new InvalidOperationException("Google did not return an access token.");
        string refreshToken = GetJsonString(document.RootElement, "refresh_token")
            ?? throw new InvalidOperationException(
                "Google did not return a refresh token. Reconnect Google Drive and grant offline access.");

        string? email = await TryGetAccountEmailAsync(accessToken, cancellationToken);
        return new GoogleOAuthTokens(accessToken, refreshToken, email);
    }

    public async Task<string> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException("Google Drive connection is invalid.");
        }

        using HttpClient client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = GetRequired("GoogleDrive:ClientId"),
            ["client_secret"] = GetRequired("GoogleDrive:ClientSecret"),
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        using HttpResponseMessage response = await client.PostAsync(
            TokenEndpoint,
            content,
            cancellationToken);

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Google Drive authorization has expired or was revoked. Please reconnect Google Drive.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return GetJsonString(document.RootElement, "access_token")
            ?? throw new InvalidOperationException("Google did not return an access token.");
    }

    public async Task<BackupHistoryItemDto> UploadBackupAsync(
        string accessToken,
        string localPath,
        string fileName,
        string integrityHash,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeAccessToken(accessToken);

        var fileInfo = new FileInfo(localPath);
        if (!fileInfo.Exists || fileInfo.Length <= 0)
        {
            throw new InvalidOperationException("Backup package was not created correctly.");
        }

        using HttpClient client = _httpClientFactory.CreateClient();
        using var initiate = new HttpRequestMessage(
            HttpMethod.Post,
            $"{DriveUploadBase}/files?uploadType=resumable&fields=id,name,size,createdTime,modifiedTime,appProperties");

        initiate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        initiate.Headers.TryAddWithoutValidation("X-Upload-Content-Type", "application/octet-stream");
        initiate.Headers.TryAddWithoutValidation("X-Upload-Content-Length", fileInfo.Length.ToString());

        string metadataJson = JsonSerializer.Serialize(new
        {
            name = fileName,
            parents = new[] { "appDataFolder" },
            mimeType = "application/octet-stream",
            appProperties = new Dictionary<string, string>
            {
                ["pdvBackup"] = "true",
                ["pdvVersion"] = "1",
                ["pdvHash"] = integrityHash
            }
        });
        initiate.Content = new StringContent(metadataJson, Encoding.UTF8, "application/json");

        using HttpResponseMessage initiated = await client.SendAsync(initiate, cancellationToken);
        if (!initiated.IsSuccessStatusCode || initiated.Headers.Location is null)
        {
            throw new InvalidOperationException("Google Drive backup upload could not be started.");
        }

        using var upload = new HttpRequestMessage(HttpMethod.Put, initiated.Headers.Location);
        upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var stream = new FileStream(
            localPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        upload.Content = new StreamContent(stream);
        upload.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        upload.Content.Headers.ContentLength = fileInfo.Length;

        using HttpResponseMessage uploaded = await client.SendAsync(
            upload,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        string resultJson = await uploaded.Content.ReadAsStringAsync(cancellationToken);
        if (!uploaded.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Google Drive backup upload failed.");
        }

        using JsonDocument resultDocument = JsonDocument.Parse(resultJson);
        return ParseDriveFile(resultDocument.RootElement, integrityHash);
    }

    public async Task<IReadOnlyList<BackupHistoryItemDto>> ListBackupsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeAccessToken(accessToken);

        string query = Uri.EscapeDataString("trashed = false and appProperties has { key='pdvBackup' and value='true' }");
        string fields = Uri.EscapeDataString("files(id,name,size,createdTime,modifiedTime,appProperties)");
        string url = $"{DriveApiBase}/files?spaces=appDataFolder&q={query}&orderBy=createdTime%20desc&fields={fields}&pageSize=100";

        using HttpClient client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Google Drive backup history could not be loaded.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("files", out JsonElement files) ||
            files.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BackupHistoryItemDto>();
        }

        var result = new List<BackupHistoryItemDto>();
        foreach (JsonElement file in files.EnumerateArray())
        {
            result.Add(ParseDriveFile(file));
        }

        return result;
    }

    public async Task<GoogleDriveDownloadedBackup> DownloadBackupAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeAccessToken(accessToken);
        ValidateFileId(fileId);

        using HttpClient client = _httpClientFactory.CreateClient();

        string metadataFields = Uri.EscapeDataString("id,name,size,appProperties");
        using var metadataRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DriveApiBase}/files/{Uri.EscapeDataString(fileId)}?fields={metadataFields}");
        metadataRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage metadataResponse = await client.SendAsync(metadataRequest, cancellationToken);
        string metadataJson = await metadataResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!metadataResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Backup file was not found in Google Drive.");
        }

        using JsonDocument metadata = JsonDocument.Parse(metadataJson);
        string? isPdvBackup = GetAppProperty(metadata.RootElement, "pdvBackup");
        if (!string.Equals(isPdvBackup, "true", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Selected Google Drive file is not a PDV backup.");
        }

        string? expectedHash = GetAppProperty(metadata.RootElement, "pdvHash");
        string tempPath = Path.Combine(Path.GetTempPath(), $"pdv-restore-{Guid.NewGuid():N}.pdvbackup");

        try
        {
            using var downloadRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{DriveApiBase}/files/{Uri.EscapeDataString(fileId)}?alt=media");
            downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await client.SendAsync(
                downloadRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Backup file could not be downloaded from Google Drive.");
            }

            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(output, cancellationToken);

            return new GoogleDriveDownloadedBackup(tempPath, expectedHash);
        }
        catch
        {
            TryDeleteFile(tempPath);
            throw;
        }
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        using HttpClient client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken
        });

        // Revocation is best-effort. Keep the refresh token in the POST body so
        // it cannot leak through request URLs, proxies or URL-oriented logs.
        try
        {
            await client.PostAsync(RevokeEndpoint, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Intentionally ignored. Local encrypted token deletion is authoritative.
        }
    }

    private async Task<string?> TryGetAccountEmailAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            return GetJsonString(document.RootElement, "email");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private BackupHistoryItemDto ParseDriveFile(JsonElement file, string? knownHash = null)
    {
        string fileId = GetJsonString(file, "id") ?? string.Empty;
        string fileName = GetJsonString(file, "name") ?? "PDV Backup";
        long size = 0;
        string? sizeText = GetJsonString(file, "size");
        _ = long.TryParse(sizeText, out size);

        DateTime? createdAt = null;
        string? createdText = GetJsonString(file, "createdTime");
        if (DateTime.TryParse(createdText, out DateTime parsedCreated))
        {
            createdAt = parsedCreated.ToUniversalTime();
        }

        return new BackupHistoryItemDto
        {
            FileId = fileId,
            FileName = fileName,
            Size = size,
            CreatedAt = createdAt,
            IntegrityHash = knownHash ?? GetAppProperty(file, "pdvHash"),
            Status = "Available"
        };
    }

    private static string? GetAppProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty("appProperties", out JsonElement properties) ||
            properties.ValueKind != JsonValueKind.Object ||
            !properties.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.GetRawText();
        }

        return null;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Drive backup is not configured on this PDV server.");
        }
    }

    private static void EnsureSafeAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Google Drive access token is missing.");
        }
    }

    private static void ValidateFileId(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId) || fileId.Length > 256)
        {
            throw new InvalidOperationException("Invalid backup file id.");
        }
    }

    private string GetRequired(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException($"Missing configuration: {key}");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp cleanup must not hide the original error.
        }
    }
}

public sealed record GoogleOAuthTokens(
    string AccessToken,
    string RefreshToken,
    string? AccountEmail);

public sealed record GoogleDriveDownloadedBackup(
    string LocalPath,
    string? ExpectedIntegrityHash);
