public static class HealthCheckResponseWriter
{
    public static Task WriteHtmlResponse(HttpContext context, HealthReport report)
    {
        var html = $@"
        <!DOCTYPE html>
        <html>
            <head>
                <title>Health Check</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    .healthy {{ color: green; }}
                    .unhealthy {{ color: red; }}
                    .degraded {{ color: orange; }}
                    table {{ border-collapse: collapse; width: 100%; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                </style>
            </head>
            <body>
                <h1>{(report.Status == HealthStatus.Healthy ? "✅ Healthy" : 
                     report.Status == HealthStatus.Degraded ? "⚠️ Degraded" : "❌ Unhealthy")}</h1>
                <h2>Status: <span class='{report.Status.ToString().ToLower()}'>
                    {report.Status}</span></h2>
                <p>Total duration: {report.TotalDuration.TotalMilliseconds} ms</p>
                <table>
                    <tr><th>Check</th><th>Status</th><th>Duration</th><th>Description</th></tr>
                    {string.Join("", report.Entries.Select(e => 
                        $"<tr><td>{e.Key}</td><td class='{e.Value.Status.ToString().ToLower()}'>" +
                        $"{e.Value.Status}</td><td>{e.Value.Duration.TotalMilliseconds} ms</td>" +
                        $"<td>{e.Value.Description ?? ""}</td></tr>"))}
                </table>
            </body>
        </html>";

        context.Response.ContentType = "text/html";
        return context.Response.WriteAsync(html);
    }
}