using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StsCompanion.Models;

namespace StsCompanion;

public static class HttpService
{
    private static HttpClient? _client;
    private static Config? _config;
    private static bool _authWarningShown;

    public static void Init(Config config)
    {
        _config = config;
        _client = new HttpClient
        {
            BaseAddress = new Uri(config.ApiUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.ApiToken);
    }

    public static async Task<RecommendResponse?> GetRecommendations(RecommendRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/companion/recommend", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleAuthError();
                return null;
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RecommendResponse>(body);
        }
        catch (TaskCanceledException)
        {
            Plugin.Log("Recommend request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            Plugin.Log($"Recommend request failed: {ex.Message}");
            return null;
        }
    }

    public static async Task SyncActiveRun(string saveJson)
    {
        if (_config?.SyncActiveRun != true) return;

        try
        {
            var content = new StringContent(saveJson, Encoding.UTF8, "application/json");
            var response = await _client!.PutAsync("/api/companion/active-run", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleAuthError();
                return;
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Plugin.Log($"Active run sync failed: {ex.Message}");
        }
    }

    public static async Task<SyncResponse?> UploadRun(string filename, string content)
    {
        if (_config?.AutoUploadRuns != true) return null;

        try
        {
            var request = new SyncRequest
            {
                Files = [new SyncFile { Filename = filename, Content = content }]
            };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/runs/sync", httpContent);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleAuthError();
                return null;
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SyncResponse>(body);
        }
        catch (Exception ex)
        {
            Plugin.Log($"Run upload failed: {ex.Message}");
            return null;
        }
    }

    public static async Task<AncientResponse?> GetAncientScores(AncientRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/companion/ancient", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleAuthError();
                return null;
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AncientResponse>(body);
        }
        catch (TaskCanceledException)
        {
            Plugin.Log("Ancient scores request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            Plugin.Log($"Ancient scores request failed: {ex.Message}");
            return null;
        }
    }

    private static void HandleAuthError()
    {
        if (_authWarningShown) return;
        _authWarningShown = true;
        Plugin.Log("ERROR: Invalid API token. Check your config.json.");
    }
}
