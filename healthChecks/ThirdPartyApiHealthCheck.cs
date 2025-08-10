public class ThirdPartyApiHealthCheck : IHealthCheck
{
    private readonly VendorApiHealthCheckSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public ThirdPartyApiHealthCheck(
        IOptions<VendorApiHealthCheckSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await client.GetAsync(
                $"{_settings.BaseUrl}/status", 
                cancellationToken);

            var latency = stopwatch.ElapsedMilliseconds;
            var isDegraded = latency > _settings.DegradedThresholdMs;

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"Vendor API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<VendorApiStatus>(content);

            var result = status?.IsHealthy == true
                ? isDegraded
                    ? HealthCheckResult.Degraded($"High latency: {latency}ms")
                    : HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Vendor API reported unhealthy status");

            result.Data.Add("LatencyMs", latency);
            result.Data.Add("Version", status?.Version);
            
            return result;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                exception: ex,
                data: new Dictionary<string, object>
                {
                    {"LatencyMs", stopwatch.ElapsedMilliseconds}
                });
        }
    }

    private class VendorApiStatus
    {
        public bool IsHealthy { get; set; }
        public string Version { get; set; }
    }
}

public class VendorApiHealthCheckSettings
{
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
    public int DegradedThresholdMs { get; set; } = 500;
}