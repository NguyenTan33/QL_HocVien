using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Implementations
{
    public class UpdateService : IUpdateService
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private readonly string _versionCheckUrl;
        private readonly int _timeoutSeconds;

        /// <summary>
        /// Cho phÃ©p giáº£ láº­p phiÃªn báº£n hiá»‡n táº¡i nháº±m phá»¥c vá»¥ kiá»ƒm thá»­ báº£n cáº­p nháº­t báº¯t buá»™c/tÃ¹y chá»n.
        /// </summary>
        public Version? SimulatedCurrentVersion { get; set; }

        static UpdateService()
        {
            if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "QL_HocVien-AutoUpdater/1.0");
            }
        }

        public UpdateService()
        {
            var config = LoadConfig();
            _versionCheckUrl = config.Url;
            _timeoutSeconds = config.TimeoutSeconds;
        }

        public UpdateService(string versionCheckUrl, int timeoutSeconds = 8)
        {
            _versionCheckUrl = versionCheckUrl;
            _timeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 8;
        }

        public Version GetCurrentVersion()
        {
            if (SimulatedCurrentVersion != null)
            {
                return SimulatedCurrentVersion;
            }

            var asmVer = Assembly.GetEntryAssembly()?.GetName().Version 
                         ?? Assembly.GetExecutingAssembly().GetName().Version;

            if (asmVer != null)
            {
                return new Version(
                    Math.Max(0, asmVer.Major),
                    Math.Max(0, asmVer.Minor),
                    Math.Max(0, asmVer.Build),
                    Math.Max(0, asmVer.Revision));
            }

            return new Version(1, 0, 0, 0);
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var currentVersion = GetCurrentVersion();
            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersion
            };

            if (string.IsNullOrWhiteSpace(_versionCheckUrl))
            {
                result.Status = UpdateStatus.CheckFailed;
                result.ErrorMessage = "ChÆ°a cáº¥u hÃ¬nh Ä‘Æ°á»ng dáº«n URL kiá»ƒm tra phiÃªn báº£n.";
                return result;
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                // Bá»• sung tham sá»‘ trÃ¡nh caching khi gá»i GitHub raw / CDN
                string requestUrl = _versionCheckUrl;
                string queryChar = requestUrl.Contains('?') ? "&" : "?";
                requestUrl += $"{queryChar}_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                using var response = await HttpClient.GetAsync(requestUrl, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    result.Status = UpdateStatus.CheckFailed;
                    result.ErrorMessage = $"MÃ¡y chá»§ kiá»ƒm tra phiÃªn báº£n pháº£n há»“i mÃ£ lá»—i: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return result;
                }

                string jsonContent = await response.Content.ReadAsStringAsync(cts.Token);
                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.LatestVersion))
                {
                    result.Status = UpdateStatus.CheckFailed;
                    result.ErrorMessage = "Dá»¯ liá»‡u cáº¥u hÃ¬nh version.json tá»« mÃ¡y chá»§ khÃ´ng há»£p lá»‡ hoáº·c thiáº¿u thÃ´ng tin phiÃªn báº£n.";
                    return result;
                }

                result.UpdateInfo = updateInfo;
                result.LatestVersion = NormalizeVersion(updateInfo.LatestVersion);
                result.MinimumVersion = NormalizeVersion(updateInfo.MinimumVersion);

                // So sÃ¡nh logic theo yÃªu cáº§u
                // 1. Náº¿u version hiá»‡n táº¡i >= latestVersion: Má»›i nháº¥t
                if (currentVersion >= result.LatestVersion)
                {
                    result.Status = UpdateStatus.UpToDate;
                }
                // 2. Náº¿u version hiá»‡n táº¡i < minimumVersion HOáº¶C mandatory = true: Báº¯t buá»™c cáº­p nháº­t
                else if (currentVersion < result.MinimumVersion || updateInfo.Mandatory)
                {
                    result.Status = UpdateStatus.MandatoryUpdateRequired;
                }
                // 3. Náº¿u version hiá»‡n táº¡i < latestVersion nhÆ°ng >= minimumVersion vÃ  mandatory = false: TÃ¹y chá»n
                else
                {
                    result.Status = UpdateStatus.OptionalUpdateAvailable;
                }

                return result;
            }
            catch (TaskCanceledException)
            {
                result.Status = UpdateStatus.CheckFailed;
                result.ErrorMessage = $"QuÃ¡ thá»i gian chá» káº¿t ná»‘i mÃ¡y chá»§ ({_timeoutSeconds} giÃ¢y). Vui lÃ²ng kiá»ƒm tra láº¡i Ä‘Æ°á»ng truyá»n Internet.";
                return result;
            }
            catch (HttpRequestException ex)
            {
                result.Status = UpdateStatus.CheckFailed;
                result.ErrorMessage = $"KhÃ´ng thá»ƒ káº¿t ná»‘i Ä‘áº¿n mÃ¡y chá»§ kiá»ƒm tra phiÃªn báº£n: {ex.Message}";
                return result;
            }
            catch (JsonException ex)
            {
                result.Status = UpdateStatus.CheckFailed;
                result.ErrorMessage = $"Lá»—i cáº¥u trÃºc tá»‡p version.json: {ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                result.Status = UpdateStatus.CheckFailed;
                result.ErrorMessage = $"Lá»—i kiá»ƒm tra phiÃªn báº£n khÃ´ng xÃ¡c Ä‘á»‹nh: {ex.Message}";
                return result;
            }
        }

        public static Version NormalizeVersion(string? versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr))
                return new Version(0, 0, 0, 0);

            versionStr = versionStr.Trim().TrimStart('v', 'V');
            var parts = versionStr.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out int mj) ? mj : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int mn) ? mn : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out int b) ? b : 0;
            int revision = parts.Length > 3 && int.TryParse(parts[3], out int r) ? r : 0;

            return new Version(major, minor, build, revision);
        }

        private (string Url, int TimeoutSeconds) LoadConfig()
        {
            string defaultUrl = "https://raw.githubusercontent.com/NguyenTan33/QL_HocVien/main/version.json";
            int defaultTimeout = 8;

            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("UpdateSettings", out var updateProp))
                    {
                        if (updateProp.TryGetProperty("VersionCheckUrl", out var urlProp) && 
                            !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            defaultUrl = urlProp.GetString()!.Trim();
                        }

                        if (updateProp.TryGetProperty("TimeoutSeconds", out var timeoutProp) && 
                            timeoutProp.TryGetInt32(out int t) && t > 0)
                        {
                            defaultTimeout = t;
                        }
                    }
                }
            }
            catch { }

            return (defaultUrl, defaultTimeout);
        }
    }
}

