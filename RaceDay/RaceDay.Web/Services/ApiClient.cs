using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    /// <summary>What came back from the API, with a friendly message for errors.</summary>
    public class ApiResponse
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public int StatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
        public bool IsSuccess => StatusCode is >= 200 and < 300;

        public T? As<T>()
        {
            if (string.IsNullOrWhiteSpace(Body)) return default;
            try { return JsonSerializer.Deserialize<T>(Body, Options); }
            catch (JsonException) { return default; }
        }

        /// <summary>A message that is safe to show to a user (never a raw error).</summary>
        public string Message
        {
            get
            {
                try
                {
                    using var doc = JsonDocument.Parse(Body);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                            return m.GetString()!;

                        if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                            foreach (var prop in errs.EnumerateObject())
                                foreach (var e in prop.Value.EnumerateArray())
                                    return e.GetString() ?? "Please check your input.";
                    }
                }
                catch (JsonException) { }

                // Some endpoints return a short plain-text message.
                var text = Body.Trim().Trim('"');
                if (StatusCode is >= 400 and < 500 && text.Length is > 0 and < 200
                    && !text.StartsWith("{") && !text.StartsWith("<"))
                    return text;

                return StatusCode switch
                {
                    400 => "Please check the details you entered.",
                    401 => "Please log in to continue.",
                    403 => "You don't have permission to do that.",
                    404 => "We couldn't find what you were looking for.",
                    409 => "That conflicts with something that already exists.",
                    503 => "The service is unavailable right now. Please try again shortly.",
                    _ => "Something went wrong. Please try again."
                };
            }
        }
    }

    /// <summary>Calls the RaceDay API and carries the API's session cookie for the logged-in user.</summary>
    public class ApiClient
    {
        private const string CookieSessionKey = "ApiSessionCookie";
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public ApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
        {
            _http = factory.CreateClient("RaceDayApi");
            _accessor = accessor;
        }

        public Task<ApiResponse> GetAsync(string url) => SendAsync(HttpMethod.Get, url, null);

        public Task<ApiResponse> PostAsync(string url, object? body = null) =>
            SendAsync(HttpMethod.Post, url, JsonBody(body));

        public Task<ApiResponse> PutAsync(string url, object body) =>
            SendAsync(HttpMethod.Put, url, JsonBody(body));

        public Task<ApiResponse> DeleteAsync(string url) => SendAsync(HttpMethod.Delete, url, null);

        /// <summary>Sends a file to the API as multipart/form-data (field name "file").</summary>
        public async Task<ApiResponse> UploadAsync(string url, IFormFile file)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);
            return await SendAsync(HttpMethod.Post, url, content);
        }

        private static StringContent JsonBody(object? body) =>
            new(JsonSerializer.Serialize(body ?? new { }, Options), Encoding.UTF8, "application/json");

        private async Task<ApiResponse> SendAsync(HttpMethod method, string url, HttpContent? content)
        {
            var session = _accessor.HttpContext!.Session;
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = session.GetString(CookieSessionKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie);

            try
            {
                using var response = await _http.SendAsync(request);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var apiSession = cookies.FirstOrDefault(c => c.StartsWith(".AspNetCore.Session="));
                    if (apiSession != null)
                        session.SetString(CookieSessionKey, apiSession.Split(';')[0]);
                }

                return new ApiResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = await response.Content.ReadAsStringAsync()
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new ApiResponse { StatusCode = 503 };
            }
        }
    }
}