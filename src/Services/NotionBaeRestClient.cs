using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Notion.Client;

namespace NotionBae.Services;

/// <summary>
/// Stole all this crap from the Notion.Client library because they strongly coupled the httpclient which is not good.
/// </summary>
public class NotionBaeRestClient : IRestClient
{

    private readonly HttpClient _httpClient;
    private readonly ILogger<NotionBaeRestClient> _logger;

    private readonly JsonSerializerSettings _defaultSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
    };

    public NotionBaeRestClient(HttpClient httpClient, ILogger<NotionBaeRestClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(
        string uri,
        IDictionary<string, string> queryParams = null,
        IDictionary<string, string> headers = null,
        JsonSerializerSettings serializerSettings = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(uri, HttpMethod.Get, queryParams, headers,
            cancellationToken: cancellationToken);

        return await response.ParseStreamAsync<T>(serializerSettings);
    }

    public async Task<T> PostAsync<T>(
        string uri,
        object body,
        IEnumerable<KeyValuePair<string, string>> queryParams = null,
        IDictionary<string, string> headers = null,
        JsonSerializerSettings serializerSettings = null,
        CancellationToken cancellationToken = default)
    {
        void AttachContent(HttpRequestMessage httpRequest)
        {
            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(body, _defaultSerializerSettings),
                Encoding.UTF8, "application/json");
        }

        var response = await SendAsync(uri, HttpMethod.Post, queryParams, headers, AttachContent,
            cancellationToken);

        return await response.ParseStreamAsync<T>(serializerSettings);
    }

    public async Task<T> PatchAsync<T>(
        string uri,
        object body,
        IDictionary<string, string> queryParams = null,
        IDictionary<string, string> headers = null,
        JsonSerializerSettings serializerSettings = null,
        CancellationToken cancellationToken = default)
    {
        void AttachContent(HttpRequestMessage httpRequest)
        {
            var serializedBody = JsonConvert.SerializeObject(body, _defaultSerializerSettings);
            httpRequest.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
        }

        var response = await SendAsync(uri, new HttpMethod("PATCH"), queryParams, headers, AttachContent,
            cancellationToken);

        return await response.ParseStreamAsync<T>(serializerSettings);
    }

    public async Task DeleteAsync(
        string uri,
        IDictionary<string, string> queryParams = null,
        IDictionary<string, string> headers = null,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(uri, HttpMethod.Delete, queryParams, headers, null, cancellationToken);
    }

    private static ClientOptions MergeOptions(ClientOptions options)
    {
        return new ClientOptions
        {
            AuthToken = options.AuthToken,
            BaseUrl = options.BaseUrl ?? Constants.BaseUrl,
            NotionVersion = options.NotionVersion ?? Constants.DefaultNotionVersion
        };
    }

    private async Task<Exception> BuildException(HttpResponseMessage response)
    {
        var errorBody = await response.Content.ReadAsStringAsync();

        NotionApiErrorResponse errorResponse = null;

        if (!string.IsNullOrWhiteSpace(errorBody))
        {
            try
            {
                errorResponse = JsonConvert.DeserializeObject<NotionApiErrorResponse>(errorBody);

                if (errorResponse.ErrorCode == NotionAPIErrorCode.RateLimited)
                {
                    var retryAfter = response.Headers.RetryAfter.Delta;
                    return new NotionApiRateLimitException(
                        response.StatusCode,
                        errorResponse.ErrorCode,
                        errorResponse.Message,
                        retryAfter
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when parsing the notion api response.");
            }
        }

        return new NotionApiException(response.StatusCode, errorResponse?.ErrorCode, errorResponse?.Message);
    }

    private async Task<HttpResponseMessage> SendAsync(
        string requestUri,
        HttpMethod httpMethod,
        IEnumerable<KeyValuePair<string, string>> queryParams = null,
        IDictionary<string, string> headers = null,
        Action<HttpRequestMessage> attachContent = null,
        CancellationToken cancellationToken = default)
    {
        requestUri = AddQueryString(requestUri, queryParams);

        using var httpRequest = new HttpRequestMessage(httpMethod, requestUri);

        if (headers != null)
        {
            AddHeaders(httpRequest, headers);
        }

        attachContent?.Invoke(httpRequest);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException(response);
        }

        return response;
    }

    private static void AddHeaders(HttpRequestMessage request, IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
    }

    private static string AddQueryString(string uri, IEnumerable<KeyValuePair<string, string>> queryParams)
    {
        return queryParams == null ? uri : QueryHelpers.AddQueryString(uri, queryParams);
    }
}


internal static class QueryHelpers
{
    public static string AddQueryString(string uri, string name, string value)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return AddQueryString(uri, new[] { new KeyValuePair<string, string>(name, value) });
    }

    public static string AddQueryString(string uri, IDictionary<string, string> queryParams)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (queryParams == null)
        {
            throw new ArgumentNullException(nameof(queryParams));
        }

        return AddQueryString(uri, (IEnumerable<KeyValuePair<string, string>>)queryParams);
    }

    public static string AddQueryString(
        string uri,
        IEnumerable<KeyValuePair<string, string>> queryParams)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (queryParams == null)
        {
            throw new ArgumentNullException(nameof(queryParams));
        }

        queryParams = RemoveEmptyValueQueryParams(queryParams);

        var anchorIndex = uri.IndexOf('#');
        var uriToBeAppended = uri;
        var anchorText = "";

        if (anchorIndex != -1)
        {
            anchorText = uri.Substring(anchorIndex);
            uriToBeAppended = uri.Substring(0, anchorIndex);
        }

        var queryIndex = uriToBeAppended.IndexOf('?');
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder();
        sb.Append(uriToBeAppended);

        foreach (var parameter in queryParams)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append(WebUtility.UrlEncode(parameter.Key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(parameter.Value));
            hasQuery = true;
        }

        sb.Append(anchorText);

        return sb.ToString();
    }

    private static IEnumerable<KeyValuePair<string, string>> RemoveEmptyValueQueryParams(
        IEnumerable<KeyValuePair<string, string>> queryParams)
    {
        return queryParams.Where(x => !string.IsNullOrWhiteSpace(x.Value));
    }
}

internal static class HttpResponseMessageExtensions
{
    internal static async Task<T> ParseStreamAsync<T>(
        this HttpResponseMessage response,
        JsonSerializerSettings serializerSettings = null)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(stream);
        using JsonReader jsonReader = new JsonTextReader(streamReader);

        var serializer = serializerSettings == null
            ? JsonSerializer.CreateDefault()
            : JsonSerializer.Create(serializerSettings);

        return serializer.Deserialize<T>(jsonReader);
    }
}
internal class NotionApiErrorResponse
{
    [JsonProperty("code")]
    public NotionAPIErrorCode ErrorCode { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
internal static class Constants
{
    internal const string BaseUrl = "https://api.notion.com/";
    internal const string DefaultNotionVersion = "2022-06-28";
}