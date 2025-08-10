public class OverallSystemHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IHealthCheck> _checks;
    
    public OverallSystemHealthCheck(IEnumerable<IHealthCheck> checks)
        => _checks = checks;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(
            _checks.Select(c => c.CheckHealthAsync(context, cancellationToken)));
        
        var unhealthy = results.Where(r => r.Status != HealthStatus.Healthy);
        
        return unhealthy.Any()
            ? HealthCheckResult.Degraded(
                $"{unhealthy.Count()} subsystems degraded")
            : HealthCheckResult.Healthy();
    }
}