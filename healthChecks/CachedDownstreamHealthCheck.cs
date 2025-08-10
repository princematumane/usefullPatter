public class CachedDownstreamHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;
    private readonly ThirdPartyApiHealthCheck _innerCheck;
    
    public CachedDownstreamHealthCheck(
        IMemoryCache cache,
        IOptions<VendorApiHealthCheckSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _cache = cache;
        _innerCheck = new ThirdPartyApiHealthCheck(settings, httpClientFactory);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("VendorApiHealth", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await _innerCheck.CheckHealthAsync(context, cancellationToken);
        });
    }
}